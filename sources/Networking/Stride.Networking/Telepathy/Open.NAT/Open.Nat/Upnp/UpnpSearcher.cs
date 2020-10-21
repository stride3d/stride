//
// Authors:
//   Ben Motmans <ben.motmans@gmail.com>
//   Lucas Ontivero lucasontivero@gmail.com
//
// Copyright (C) 2007 Ben Motmans
// Copyright (C) 2014 Lucas Ontivero
//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
// 
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Net;
using System.Diagnostics;
using System.Net.Sockets;
using System.Threading;
using System.Xml;

namespace Open.Nat
{
	internal class UpnpSearcher : Searcher
	{
		private readonly IIPAddressesProvider _ipprovider;
		private readonly IDictionary<Uri, NatDevice> _devices;
		private readonly Dictionary<IPAddress, DateTime> _lastFetched;
		private static readonly string[] ServiceTypes = new[]{
			"WANIPConnection:2", 
			"WANPPPConnection:2",
			"WANIPConnection:1", 
			"WANPPPConnection:1" 
		};

		internal UpnpSearcher(IIPAddressesProvider ipprovider)
		{
			_ipprovider = ipprovider;
			UdpClients = CreateUdpClients();
			_devices = new Dictionary<Uri, NatDevice>();
			_lastFetched = new Dictionary<IPAddress, DateTime>();
		}

		private List<UdpClient> CreateUdpClients()
		{
			var clients = new List<UdpClient>();
			try
			{
				var ips = _ipprovider.UnicastAddresses();

				foreach (var ipAddress in ips)
				{
					try
					{
						clients.Add(new UdpClient(new IPEndPoint(ipAddress, 0)));
					}
					catch (Exception)
					{
						continue; // Move on to the next address.
					}
				}
			}
			catch (Exception)
			{
				clients.Add(new UdpClient(0));
			}
			return clients;
		}

		protected override void Discover(UdpClient client, CancellationToken cancelationToken)
		{
			// for testing use: 
			//    <code>var ip = IPAddress.Broadcast;</code>
			Discover(client, WellKnownConstants.IPv4MulticastAddress, cancelationToken);
			if (Socket.OSSupportsIPv6)
			{
				Discover(client, WellKnownConstants.IPv6LinkLocalMulticastAddress, cancelationToken);
				Discover(client, WellKnownConstants.IPv6LinkSiteMulticastAddress, cancelationToken);
			}
		}

		private void Discover(UdpClient client, IPAddress address, CancellationToken cancelationToken)
		{
			if (!IsValidClient(client.Client, address)) return;
			
			NextSearch = DateTime.UtcNow.AddSeconds(1);
			var searchEndpoint = new IPEndPoint(address, 1900);

			foreach (var serviceType in ServiceTypes)
			{
				var datax = DiscoverDeviceMessage.Encode(serviceType, address);
				var data = Encoding.ASCII.GetBytes(datax);

				// UDP is unreliable, so send 3 requests at a time (per Upnp spec, sec 1.1.2)
				// Yes, however it works perfectly well with just 1 request.
				for (var i = 0; i < 3; i++)
				{
					if (cancelationToken.IsCancellationRequested) return;
					client.Send(data, data.Length, searchEndpoint);
				}
			}
		}

		private bool IsValidClient(Socket socket, IPAddress address)
		{
			var endpoint = (IPEndPoint) socket.LocalEndPoint;
			if (socket.AddressFamily != address.AddressFamily) return false;

			switch (socket.AddressFamily)
			{
				case AddressFamily.InterNetwork:
					socket.SetSocketOption(SocketOptionLevel.IP, SocketOptionName.MulticastInterface, endpoint.Address.GetAddressBytes());
					return true;
				case AddressFamily.InterNetworkV6:
					if (endpoint.Address.IsIPv6LinkLocal && !Equals(address, WellKnownConstants.IPv6LinkLocalMulticastAddress))
						return false;
					if (!endpoint.Address.IsIPv6LinkLocal && !Equals(address, WellKnownConstants.IPv6LinkSiteMulticastAddress))
						return false;

					socket.SetSocketOption(SocketOptionLevel.IPv6, SocketOptionName.MulticastInterface, BitConverter.GetBytes((int)endpoint.Address.ScopeId));
					return true;
			}
			return false;
		}

		public override NatDevice AnalyseReceivedResponse(IPAddress localAddress, byte[] response, IPEndPoint endpoint)
		{
			// Convert it to a string for easy parsing
			string dataString = null;

			// No matter what, this method should never throw an exception. If something goes wrong
			// we should still be in a position to handle the next reply correctly.
			try
			{
				dataString = Encoding.UTF8.GetString(response);
				var message = new DiscoveryResponseMessage(dataString);
				var serviceType = message["ST"];

				if (!IsValidControllerService(serviceType))
				{
					NatDiscoverer.TraceSource.LogWarn("Invalid controller service. Ignoring.");

					return null;
				}
				NatDiscoverer.TraceSource.LogInfo("UPnP Response: Router advertised a '{0}' service!!!", serviceType);

				var location = message["Location"] ?? message["AL"];
				var locationUri = new Uri(location);
				NatDiscoverer.TraceSource.LogInfo("Found device at: {0}", locationUri.ToString());

				if (_devices.ContainsKey(locationUri))
				{
					NatDiscoverer.TraceSource.LogInfo("Already found - Ignored");
					_devices[locationUri].Touch();
					return null;
				}

				// If we send 3 requests at a time, ensure we only fetch the services list once
				// even if three responses are received
				if (_lastFetched.ContainsKey(endpoint.Address))
				{
					var last = _lastFetched[endpoint.Address];
					if ((DateTime.Now - last) < TimeSpan.FromSeconds(20))
						return null;
				}
				_lastFetched[endpoint.Address] = DateTime.Now;

				NatDiscoverer.TraceSource.LogInfo("{0}:{1}: Fetching service list", locationUri.Host, locationUri.Port );

				var deviceInfo = BuildUpnpNatDeviceInfo(localAddress, locationUri);

				UpnpNatDevice device;
				lock (_devices)
				{
					device = new UpnpNatDevice(deviceInfo);
					if (!_devices.ContainsKey(locationUri))
					{
						_devices.Add(locationUri, device);
					}
				}
				return device;
			}
			catch (Exception ex)
			{
				NatDiscoverer.TraceSource.LogError("Unhandled exception when trying to decode a device's response. ");
				NatDiscoverer.TraceSource.LogError("Report the issue in https://github.com/lontivero/Open.Nat/issues");
				NatDiscoverer.TraceSource.LogError("Also copy and paste the following info:");
				NatDiscoverer.TraceSource.LogError("-- beging ---------------------------------");
				NatDiscoverer.TraceSource.LogError(ex.Message);
				NatDiscoverer.TraceSource.LogError("Data string:");
				NatDiscoverer.TraceSource.LogError(dataString ?? "No data available");
				NatDiscoverer.TraceSource.LogError("-- end ------------------------------------");
			}
			return null;
		}

		private static bool IsValidControllerService(string serviceType)
		{
			var services = from serviceName in ServiceTypes
						   let serviceUrn = string.Format("urn:schemas-upnp-org:service:{0}", serviceName)
						   where serviceType.ContainsIgnoreCase(serviceUrn)
						   select new {ServiceName = serviceName, ServiceUrn = serviceUrn};

			return services.Any();
		}

		private UpnpNatDeviceInfo BuildUpnpNatDeviceInfo(IPAddress localAddress, Uri location)
		{
			NatDiscoverer.TraceSource.LogInfo("Found device at: {0}", location.ToString());

			var hostEndPoint = new IPEndPoint(IPAddress.Parse(location.Host), location.Port);

			WebResponse response = null;
			try
			{
#if NET35
				var request = WebRequest.Create(location);
#else
				var request = WebRequest.CreateHttp(location);
#endif
				request.Headers.Add("ACCEPT-LANGUAGE", "en");
				request.Method = "GET";

				response = request.GetResponse();

				var httpresponse = response as HttpWebResponse;

				if (httpresponse != null && httpresponse.StatusCode != HttpStatusCode.OK)
				{
					var message = string.Format("Couldn't get services list: {0} {1}", httpresponse.StatusCode, httpresponse.StatusDescription);
					throw new Exception(message);
				}

				var xmldoc = ReadXmlResponse(response);

				NatDiscoverer.TraceSource.LogInfo("{0}: Parsed services list", hostEndPoint);

				var ns = new XmlNamespaceManager(xmldoc.NameTable);
				ns.AddNamespace("ns", "urn:schemas-upnp-org:device-1-0");
				var services = xmldoc.SelectNodes("//ns:service", ns);

				foreach (XmlNode service in services)
				{
					var serviceType = service.GetXmlElementText("serviceType");
					if (!IsValidControllerService(serviceType)) continue;

					NatDiscoverer.TraceSource.LogInfo("{0}: Found service: {1}", hostEndPoint, serviceType);

					var serviceControlUrl = service.GetXmlElementText("controlURL");
					NatDiscoverer.TraceSource.LogInfo("{0}: Found upnp service at: {1}", hostEndPoint, serviceControlUrl);

					NatDiscoverer.TraceSource.LogInfo("{0}: Handshake Complete", hostEndPoint);
					return new UpnpNatDeviceInfo(localAddress, location, serviceControlUrl, serviceType);
				}

				throw new Exception("No valid control service was found in the service descriptor document");
			}
			catch (WebException ex)
			{
				// Just drop the connection, FIXME: Should i retry?
				NatDiscoverer.TraceSource.LogError("{0}: Device denied the connection attempt: {1}", hostEndPoint, ex);
				var inner = ex.InnerException as SocketException;
				if (inner != null)
				{
					NatDiscoverer.TraceSource.LogError("{0}: ErrorCode:{1}", hostEndPoint, inner.ErrorCode);
					NatDiscoverer.TraceSource.LogError("Go to http://msdn.microsoft.com/en-us/library/system.net.sockets.socketerror.aspx");
					NatDiscoverer.TraceSource.LogError("Usually this happens. Try resetting the device and try again. If you are in a VPN, disconnect and try again.");
				}
				throw;
			}
			finally
			{
				if (response != null)
					response.Close();
			}
		}

		private static XmlDocument ReadXmlResponse(WebResponse response)
		{
			using (var reader = new StreamReader(response.GetResponseStream(), Encoding.UTF8))
			{
				var servicesXml = reader.ReadToEnd();
				var xmldoc = new XmlDocument();
				xmldoc.LoadXml(servicesXml);
				return xmldoc;
			}
		}
	}
}
