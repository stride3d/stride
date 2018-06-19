#!/usr/bin/python
# -*- coding: utf-8 -*-
#
#	usbmux.py - usbmux client library for Python
#
# Copyright (C) 2009	Hector Martin "marcan" <hector@marcansoft.com>
#
# This program is free software; you can redistribute it and/or modify
# it under the terms of the GNU General Public License as published by
# the Free Software Foundation, either version 2 or version 3.
#
# This program is distributed in the hope that it will be useful,
# but WITHOUT ANY WARRANTY; without even the implied warranty of
# MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
# GNU General Public License for more details.
#
# You should have received a copy of the GNU General Public License
# along with this program; if not, write to the Free Software
# Foundation, Inc., 51 Franklin Street, Fifth Floor, Boston, MA  02110-1301  USA

import socket, struct, select, sys

try:
	import plistlib
	haveplist = True
except:
	haveplist = False

class MuxError(Exception):
	pass

class MuxVersionError(MuxError):
	pass

class SafeStreamSocket:
	def __init__(self, address, family):
		self.sock = socket.socket(family, socket.SOCK_STREAM)
		self.sock.connect(address)
	def send(self, msg):
		totalsent = 0
		while totalsent < len(msg):
			sent = self.sock.send(msg[totalsent:])
			if sent == 0:
				raise MuxError("socket connection broken")
			totalsent = totalsent + sent
	def recv(self, size):
		msg = ''
		while len(msg) < size:
			chunk = self.sock.recv(size-len(msg))
			if chunk == '':
				raise MuxError("socket connection broken")
			msg = msg + chunk
		return msg

class MuxDevice(object):
	def __init__(self, devid, usbprod, serial, location):
		self.devid = devid
		self.usbprod = usbprod
		self.serial = serial
		self.location = location
	def __str__(self):
		return "<MuxDevice: ID %d ProdID 0x%04x Serial '%s' Location 0x%x>"%(self.devid, self.usbprod, self.serial, self.location)

class BinaryProtocol(object):
	TYPE_RESULT = 1
	TYPE_CONNECT = 2
	TYPE_LISTEN = 3
	TYPE_DEVICE_ADD = 4
	TYPE_DEVICE_REMOVE = 5
	VERSION = 0
	def __init__(self, socket):
		self.socket = socket
		self.connected = False

	def _pack(self, req, payload):
		if req == self.TYPE_CONNECT:
			return struct.pack("IH", payload['DeviceID'], payload['PortNumber']) + "\x00\x00"
		elif req == self.TYPE_LISTEN:
			return ""
		else:
			raise ValueError("Invalid outgoing request type %d"%req)
	
	def _unpack(self, resp, payload):
		if resp == self.TYPE_RESULT:
			return {'Number':struct.unpack("I", payload)[0]}
		elif resp == self.TYPE_DEVICE_ADD:
			devid, usbpid, serial, pad, location = struct.unpack("IH256sHI", payload)
			serial = serial.split("\0")[0]
			return {'DeviceID': devid, 'Properties': {'LocationID': location, 'SerialNumber': serial, 'ProductID': usbpid}}
		elif resp == self.TYPE_DEVICE_REMOVE:
			devid = struct.unpack("I", payload)[0]
			return {'DeviceID': devid}
		else:
			raise MuxError("Invalid incoming request type %d"%req)

	def sendpacket(self, req, tag, payload={}):
		payload = self._pack(req, payload)
		if self.connected:
			raise MuxError("Mux is connected, cannot issue control packets")
		length = 16 + len(payload)
		data = struct.pack("IIII", length, self.VERSION, req, tag) + payload
		self.socket.send(data)
	def getpacket(self):
		if self.connected:
			raise MuxError("Mux is connected, cannot issue control packets")
		dlen = self.socket.recv(4)
		dlen = struct.unpack("I", dlen)[0]
		body = self.socket.recv(dlen - 4)
		version, resp, tag = struct.unpack("III",body[:0xc])
		if version != self.VERSION:
			raise MuxVersionError("Version mismatch: expected %d, got %d"%(self.VERSION,version))
		payload = self._unpack(resp, body[0xc:])
		return (resp, tag, payload)

class PlistProtocol(BinaryProtocol):
	TYPE_RESULT = "Result"
	TYPE_CONNECT = "Connect"
	TYPE_LISTEN = "Listen"
	TYPE_DEVICE_ADD = "Attached"
	TYPE_DEVICE_REMOVE = "Detached" #???
	TYPE_PLIST = 8
	VERSION = 1
	def __init__(self, socket):
		if not haveplist:
			raise Exception("You need the plistlib module")
		BinaryProtocol.__init__(self, socket)
	
	def _pack(self, req, payload):
		return payload
	
	def _unpack(self, resp, payload):
		return payload
	
	def sendpacket(self, req, tag, payload={}):
		payload['ClientVersionString'] = 'usbmux.py by marcan'
		if isinstance(req, int):
			req = [self.TYPE_CONNECT, self.TYPE_LISTEN][req-2]
		payload['MessageType'] = req
		payload['ProgName'] = 'tcprelay'
		BinaryProtocol.sendpacket(self, self.TYPE_PLIST, tag, plistlib.writePlistToString(payload))
	def getpacket(self):
		resp, tag, payload = BinaryProtocol.getpacket(self)
		if resp != self.TYPE_PLIST:
			raise MuxError("Received non-plist type %d"%resp)
		payload = plistlib.readPlistFromString(payload)
		return payload['MessageType'], tag, payload

class MuxConnection(object):
	callback_device_added = None
	callback_device_removed = None

	def __init__(self, socketpath, protoclass):
		self.socketpath = socketpath
		if sys.platform in ['win32', 'cygwin']:
			family = socket.AF_INET
			address = ('127.0.0.1', 27015)
		else:
			family = socket.AF_UNIX
			address = self.socketpath
		self.socket = SafeStreamSocket(address, family)
		self.proto = protoclass(self.socket)
		self.pkttag = 1
		self.devices = []

	def _getreply(self):
		while True:
			resp, tag, data = self.proto.getpacket()
			if resp == self.proto.TYPE_RESULT:
				return tag, data
			else:
				raise MuxError("Invalid packet type received: %d"%resp)
	def _processpacket(self):
		resp, tag, data = self.proto.getpacket()
		if resp == self.proto.TYPE_DEVICE_ADD:
			dev = MuxDevice(data['DeviceID'], data['Properties']['ProductID'], data['Properties']['SerialNumber'], data['Properties']['LocationID'])
			self.devices.append(dev)
			if self.callback_device_added != None:
				self.callback_device_added(dev)
		elif resp == self.proto.TYPE_DEVICE_REMOVE:
			for dev in self.devices:
				if dev.devid == data['DeviceID']:
					if self.callback_device_removed != None:
						self.callback_device_removed(dev)
					self.devices.remove(dev)
		elif resp == self.proto.TYPE_RESULT:
			raise MuxError("Unexpected result: %d"%resp)
		else:
			raise MuxError("Invalid packet type received: %d"%resp)
	def _exchange(self, req, payload={}):
		mytag = self.pkttag
		self.pkttag += 1
		self.proto.sendpacket(req, mytag, payload)
		recvtag, data = self._getreply()
		if recvtag != mytag:
			raise MuxError("Reply tag mismatch: expected %d, got %d"%(mytag, recvtag))
		return data['Number']

	def listen(self):
		ret = self._exchange(self.proto.TYPE_LISTEN)
		if ret != 0:
			raise MuxError("Listen failed: error %d"%ret)
	def process(self, timeout=None):
		if self.proto.connected:
			raise MuxError("Socket is connected, cannot process listener events")
		rlo, wlo, xlo = select.select([self.socket.sock], [], [self.socket.sock], timeout)
		if xlo:
			self.socket.sock.close()
			raise MuxError("Exception in listener socket")
		if rlo:
			self._processpacket()
	def connect(self, device, port):
		ret = self._exchange(self.proto.TYPE_CONNECT, {'DeviceID':device.devid, 'PortNumber':((port<<8) & 0xFF00) | (port>>8)})
		if ret != 0:
			raise MuxError("Connect failed: error %d"%ret)
		self.proto.connected = True
		return self.socket.sock
	def close(self):
		self.socket.sock.close()

class USBMux(object):
	def __init__(self, socketpath=None):
		if socketpath is None:
			if sys.platform == 'darwin':
				socketpath = "/var/run/usbmuxd"
			else:
				socketpath = "/var/run/usbmuxd"
		self.socketpath = socketpath
		self.listener = MuxConnection(socketpath, BinaryProtocol)
		try:
			self.listener.listen()
			self.version = 0
			self.protoclass = BinaryProtocol
		except MuxVersionError:
			self.listener = MuxConnection(socketpath, PlistProtocol)
			self.listener.listen()
			self.protoclass = PlistProtocol
			self.version = 1
		self.devices = self.listener.devices
	def process(self, timeout=None):
		self.listener.process(timeout)
	def connect(self, device, port):
		connector = MuxConnection(self.socketpath, self.protoclass)
		return connector.connect(device, port)

if __name__ == "__main__":
	mux = USBMux()
	print "Waiting for devices..."
	if not mux.devices:
		mux.process(0.1)
	while True:
		print "Devices:"
		for dev in mux.devices:
			print dev
		mux.process()
