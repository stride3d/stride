#!/usr/bin/python
# -*- coding: utf-8 -*-
#
#	stride-ios-relay.py - Stride TCP connection relay for iOS devices to Windows developer host (using usbmuxd)
#
# Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
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

import usbmux
import SocketServer
import select
from optparse import OptionParser
import sys
import threading
import time
import traceback
import socket

class SocketRelay(object):
	def __init__(self, a, b, maxbuf=65535):
		self.a = a
		self.b = b
		self.atob = ""
		self.btoa = ""
		self.maxbuf = maxbuf
	def handle(self):
		while True:
			rlist = []
			wlist = []
			xlist = [self.a, self.b]
			if self.atob:
				wlist.append(self.b)
			if self.btoa:
				wlist.append(self.a)
			if len(self.atob) < self.maxbuf:
				rlist.append(self.a)
			if len(self.btoa) < self.maxbuf:
				rlist.append(self.b)
			rlo, wlo, xlo = select.select(rlist, wlist, xlist)
			if xlo:
				return
			if self.a in wlo:
				n = self.a.send(self.btoa)
				self.btoa = self.btoa[n:]
			if self.b in wlo:
				n = self.b.send(self.atob)
				self.atob = self.atob[n:]
			if self.a in rlo:
				s = self.a.recv(self.maxbuf - len(self.atob))
				if not s:
					return
				self.atob += s
			if self.b in rlo:
				s = self.b.recv(self.maxbuf - len(self.btoa))
				if not s:
					return
				self.btoa += s
			#print "Relay iter: %8d atob, %8d btoa, lists: %r %r %r"%(len(self.atob), len(self.btoa), rlo, wlo, xlo)

parser = OptionParser(usage="usage: %prog [OPTIONS] RemoteHost")
parser.add_option("-b", "--bufsize", dest='bufsize', action='store', metavar='KILOBYTES', type='int', default=16, help="specify buffer size for socket forwarding")
parser.add_option("-s", "--socket", dest='sockpath', action='store', metavar='PATH', type='str', default=None, help="specify the path of the usbmuxd socket")

options, args = parser.parse_args()

if len(args) != 1:
	parser.print_help()
	sys.exit(1)

alive = True

remotehost = args[0]

mux = usbmux.USBMux(options.sockpath)

class DeviceConnectionHelper():
	def __init__(self, device):
		self.device = device
	def start_connection(self, device_sock):
		try:
			print "Connection opened with device, establishing connection to router (%s)"%(remotehost)
	
			# Connect to router
			router_sock = socket.socket()
			router_sock.connect((remotehost, 31254))
			
			print "Starting relay between iOS device and router"
			
			# Forward connection between router and iOS device
			fwd = SocketRelay(device_sock, router_sock, options.bufsize * 1024)
			fwd.handle()
		except:
			traceback.print_exc(file=sys.stdout)
			pass
		finally:
			print "Connection between iOS device and router has been interrupted"
			device_sock.close()
			router_sock.close()
	
	def start_device(self):
		self.device.alive = True
		while self.device.alive and alive:
			try:
				device_sock = mux.connect(self.device, 31255)

				# Start a thread for this connection
				thread = threading.Thread(target = lambda: self.start_connection(device_sock))
				thread.start()
			except:
				# Silently ignore exceptions (since we try to continuously connect to device)
				pass
			time.sleep(0.2)
	def start_device_threaded(self):
		thread = threading.Thread(target = self.start_device)
		thread.start()


deviceNames = {
0x1290: 'iPhone',
0x1292: 'iPhone 3G',
0x1294: 'iPhone 3GS',
0x1297: 'iPhone 4 GSM',
0x129c: 'iPhone 4 CDMA',
0x12a0: 'iPhone 4S',
0x12a8: 'iPhone 5/6',
0x1291: 'iPod touch',
0x1293: 'iPod touch 2G',
0x1299: 'iPod touch 3G',
0x129e: 'iPod touch 4G',
0x129a: 'iPad',
0x129f: 'iPad 2 Wi-Fi',
0x12a2: 'iPad 2 GSM',
0x12a3: 'iPad 2 CDMA',
0x12a9: 'iPad 2 R2',
0x12a4: 'iPad 3 Wi-Fi',
0x12a5: 'iPad 3 CDMA',
0x12a6: 'iPad 3 Global',
0x129d: 'Apple TV 2G',
0x12a7: 'Apple TV 3G'
}

def device_name(device):
	return deviceNames.get(device.usbprod, "Unknown(0x%04x)"%(device.usbprod))

def device_added(device):
	# Try to connect to establish connection to device
	print "Device connected: ID %d, Type %s (Serial %s)"%(device.devid, device_name(device), device.serial)
	deviceConnectionHelper = DeviceConnectionHelper(device)
	deviceConnectionHelper.start_device_threaded()

def device_removed(device):
	print "Device removed: ID %d, Type %s (Serial %s)"%(device.devid, device_name(device), device.serial)
	device.alive = False

print "Listening for iOS devices..."
mux.listener.callback_device_added = device_added
mux.listener.callback_device_removed = device_removed

alive = True

while alive:
	try:
		mux.process()
	except:
		alive = False
