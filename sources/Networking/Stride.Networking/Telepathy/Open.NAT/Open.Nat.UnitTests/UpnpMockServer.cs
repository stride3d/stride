using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace Open.Nat.Tests
{
	public class ServerConfiguration
	{
		public ServerConfiguration()
		{
			ServiceType = "WANIPConnection:1";
			Prefix = "http://127.0.0.1:5431/";
			ServiceUrl = "/dyndev/uuid:0000e068-20a0-00e0-20a0-48a8000808e0";
			ControlUrl = "/uuid:0000e068-20a0-00e0-20a0-48a802086048/" + ServiceType;
		}

		public string ControlUrl { get; set; }

		public string ServiceUrl { get; set; }

		public string ServiceType { get; set; }

		public string Prefix { get; set; }
	}

	public class UpnpMockServer : IDisposable
	{
		private readonly HttpListener _listener;
		private ServerConfiguration _cfg;
		public Action<HttpListenerContext> WhenRequestServiceDesc = WhenRequestService;
		public Action<HttpListenerContext> WhenGetExternalIpAddress = ResponseOk;
		public Action<HttpListenerContext> WhenAddPortMapping = ResponseOk;
		public Action<HttpListenerContext> WhenGetGenericPortMappingEntry = ResponseOk;
		public Action<HttpListenerContext> WhenDeletePortMapping = ResponseOk;
		public Func<string> WhenDiscoveryRequest;

		private string HandleDiscoveryRequest()
		{
			return "HTTP/1.1 200 OK\r\n"
					+ "Server: Custom/1.0 UPnP/1.0 Proc/Ver\r\n"
					+ "EXT:\r\n"
					+ "Location: " + _cfg.ServiceUrl + "\r\n"
					+ "Cache-Control:max-age=1800\r\n"
					+ "ST:urn:schemas-upnp-org:service:" + _cfg.ServiceType + "\r\n"
					+
					"USN:uuid:0000e068-20a0-00e0-20a0-48a802086048::urn:schemas-upnp-org:service:" + _cfg.ServiceType;
		}

		private static void ResponseOk(HttpListenerContext context)
		{
			context.Response.Status(200, "OK");
		}

		private static void WhenRequestService(HttpListenerContext context)
		{
			var responseBytes = File.OpenRead("..\\..\\Responses\\ServiceDescription.txt");
#if NET35
			var buffer = new byte[1024];
			int count;
			while ((count = responseBytes.Read(buffer, 0, buffer.Length)) != 0)
			{
				context.Response.OutputStream.Write(buffer, 0, count);
			}
#else
			responseBytes.CopyTo(context.Response.OutputStream);
#endif
			context.Response.OutputStream.Flush();

			context.Response.Status(200, "OK");
		}
		
		public UpnpMockServer()
			: this (new ServerConfiguration())
		{ }

		public UpnpMockServer(ServerConfiguration cfg)
		{
			_cfg = cfg;
			_listener = new HttpListener();
			_listener.Prefixes.Add(cfg.Prefix);
			_listener.AuthenticationSchemes = AuthenticationSchemes.Anonymous;
			WhenDiscoveryRequest = HandleDiscoveryRequest;
		}

		public void Start()
		{
			StartAnnouncer();
			StartServer();
		}

		private void StartAnnouncer()
		{
#if NET35
			Task.Factory.StartNew(
#else
			Task.Run(
#endif
				() =>{
				var remoteIPEndPoint = new IPEndPoint(IPAddress.Any, 0);
				using (var udpClient = new UdpClient(1900))
				{
					while (true)
					{
						var bytes = udpClient.Receive(ref remoteIPEndPoint);
						if (bytes == null || bytes.Length == 0) return;

						var response = WhenDiscoveryRequest();

						var responseBytes = Encoding.UTF8.GetBytes(response);
						udpClient.Send(responseBytes, responseBytes.Length, remoteIPEndPoint);
					}
				}
			});

			//Task.Run(() => {
			//	var remoteIPEndPoint = new IPEndPoint(IPAddress.IPv6Any, 0);
			//	using (var udpClient = new UdpClient(1900))
			//	{
			//		while (true)
			//		{
			//			var bytes = udpClient.Receive(ref remoteIPEndPoint);
			//			if (bytes == null || bytes.Length == 0) return;

			//			var response = WhenDiscoveryRequest();

			//			var responseBytes = Encoding.UTF8.GetBytes(response);
			//			udpClient.Send(responseBytes, responseBytes.Length, remoteIPEndPoint);
			//		}
			//	}
			//});
		}

		private void StartServer()
		{
			_listener.Start();
#if NET35
			Task.Factory.StartNew(
#else
			Task.Run(
#endif
			() => {
				while (true)
				{
					ProcessRequest();
				}
			});
		}

		private void ProcessRequest()
		{
			var result = _listener.BeginGetContext(ListenerCallback, _listener);
			result.AsyncWaitHandle.WaitOne();
		}

		private void ListenerCallback(IAsyncResult result)
		{
			if(!_listener.IsListening) return;
			var context = _listener.EndGetContext(result);
			var request = context.Request;
			if(request.Url.AbsoluteUri == _cfg.ServiceUrl)
			{
				WhenRequestServiceDesc(context);
				return;
			}
			
			if(request.Url.AbsoluteUri == _cfg.ControlUrl)
			{
				var soapActionHeader = request.Headers["SOAPACTION"];
				soapActionHeader = soapActionHeader.Substring(1, soapActionHeader.Length - 2);

				var soapActionHeaderParts = soapActionHeader.Split(new[] { '#' });
				var serviceType = soapActionHeaderParts[0];
				var soapAction = soapActionHeaderParts[1];
				var buffer = new byte[request.ContentLength64-4];
				request.InputStream.Read(buffer, 0, buffer.Length);
				var body = Encoding.UTF8.GetString(buffer);
				var envelop = XElement.Parse(body);

				switch (soapAction)
				{
					case "GetExternalIPAddress":
						WhenGetExternalIpAddress(context);
						return;
					case "AddPortMapping":
						WhenAddPortMapping(context);
						return;
					case "GetGenericPortMappingEntry":
						WhenGetGenericPortMappingEntry(context);
						return;
					case "DeletePortMapping":
						WhenDeletePortMapping(context);
						return;
				}
				context.Response.Status(200, "OK");
				return;
			}
			context.Response.Status(500, "Internal Server Error");
		}

		//private void ProcessGetGenericPortMappingEntry(XElement envelop, HttpListenerResponse response)
		//{
		//	var env = envelop.Descendants(XName.Get("{urn:schemas-upnp-org:service:" + _cfg.ServiceType + "}GetGenericPortMappingEntry")).First();
		//	var vals = env.Elements().ToDictionary(x => x.Name.LocalName, x => x.Value);

		//	try
		//	{
		//		var e = _table[int.Parse(vals["NewPortMappingIndex"])];
		//		var responseXml = @"<?xml version=""1.0""?>
		//		<s:Envelope xmlns:s=""http://schemas.xmlsoap.org/soap/envelope/"" 
		//					s:encodingStyle=""http://schemas.xmlsoap.org/soap/encoding/"">
		//			<s:Body>
		//			<m:GetGenericPortMappingEntryResponse xmlns:m=""urn:schemas-upnp-org:service:" + _st + @""">
		//				  <NewRemoteHost>" + e.NewRemoteHost + @"</NewRemoteHost>
		//				  <NewExternalPort>" + e.NewExternalPort + @"</NewExternalPort>
		//				  <NewProtocol>" + e.NewProtocol + @"</NewProtocol>
		//				  <NewInternalPort>" + e.NewInternalPort + @"</NewInternalPort>
		//				  <NewInternalClient>" + e.NewInternalClient + @"</NewInternalClient>
		//				  <NewEnabled>"  + e.NewEnabled +  @"</NewEnabled>
		//				  <NewPortMappingDescription>"+ e.NewPortMappingDescription + @"</NewPortMappingDescription>
		//				  <NewLeaseDuration>" + e.NewLeaseDuration+ @"</NewLeaseDuration>
		//			</m:GetGenericPortMappingEntryResponse>
		//			</s:Body>
		//		</s:Envelope>";

		//		var bytes = Encoding.UTF8.GetBytes(responseXml);
		//		response.OutputStream.Write(bytes, 0, bytes.Length);
		//		response.OutputStream.Flush();
		//		response.StatusCode = 200;
		//		response.StatusDescription = "OK";
		//		response.Close();
		//	}
		//	catch
		//	{
		//		Error(713, "SpecifiedArrayIndexInvalid", response);
		//	}
		//}

		//private void processAddPortMapping(XElement envelop, HttpListenerResponse response)
		//{
		//	var e = envelop.Descendants(XName.Get("{urn:schemas-upnp-org:service:" + _st + "}AddPortMapping")).First();
		//	var vals = e.Elements().ToDictionary(x => x.Name.LocalName, x=> x.Value);
		//	if(false && vals["NewLeaseDuration"]!="0")
		//	{
		//		Error(725, "OnlyPermanentLeaseSupported", response);
		//		return;
		//	}
		//	var newMapping = new Mapping {
		//		NewLeaseDuration = int.Parse(vals["NewLeaseDuration"]),
		//		NewRemoteHost = vals["NewRemoteHost"],
		//		NewExternalPort = vals["NewExternalPort"],
		//		NewProtocol = vals["NewProtocol"],
		//		NewInternalPort = vals["NewInternalPort"],
		//		NewInternalClient = vals["NewInternalClient"],
		//		NewEnabled = vals["NewEnabled"],
		//		NewPortMappingDescription = vals["NewPortMappingDescription"],
		//	};

		//	var exists = _table.Any(x => x.Equals(newMapping));
		//	if(exists )
		//	{
		//		Error(718, "ConflictMapping", response);
		//		return;
		//	}
		//	_table.Add(newMapping);
		//	var responseXml = @"<?xml version=""1.0""?>
		//		<s:Envelope xmlns:s=""http://schemas.xmlsoap.org/soap/envelope/"" 
		//					s:encodingStyle=""http://schemas.xmlsoap.org/soap/encoding/"">
		//			<s:Body>
		//			<m:AddPortMappingResponse xmlns:m=""urn:schemas-upnp-org:service:" + _st + @""">
		//			</m:AddPortMappingResponse>
		//			</s:Body>
		//		</s:Envelope>";
		//	var bytes = Encoding.UTF8.GetBytes(responseXml);
		//	response.OutputStream.Write(bytes, 0, bytes.Length);
		//	response.OutputStream.Flush();
		//	response.StatusCode = 200;
		//	response.StatusDescription = "OK";
		//	response.Close();
		//}

		//private void ProcessDeletePortMapping(XElement envelop, HttpListenerResponse response)
		//{
		//	var e = envelop.Descendants(XName.Get("{urn:schemas-upnp-org:service:" + _st + "}DeletePortMapping")).First();
		//	var vals = e.Elements().ToDictionary(x => x.Name.LocalName, x => x.Value);

		//	var delete = _table.RemoveAll(x=> x.NewProtocol == vals["NewProtocol"] &&
		//		x.NewRemoteHost == vals["NewRemoteHost"] &&
		//		x.NewExternalPort == vals["NewExternalPort"]);

		//	if(delete == 0)
		//	{
		//		Error(714, "NoSuchEntryInArray", response);
		//		return;
		//	}
		//	var responseXml = @"<?xml version=""1.0""?>
		//		<s:Envelope xmlns:s=""http://schemas.xmlsoap.org/soap/envelope/"" 
		//					s:encodingStyle=""http://schemas.xmlsoap.org/soap/encoding/"">
		//			<s:Body>
		//			<m:DeletePortMappingResponse xmlns:m=""urn:schemas-upnp-org:service:" + _st + @""">
		//			</m:DeletePortMappingResponse>
		//			</s:Body>
		//		</s:Envelope>";
		//	var bytes = Encoding.UTF8.GetBytes(responseXml);
		//	response.OutputStream.Write(bytes, 0, bytes.Length);
		//	response.OutputStream.Flush();
		//	response.StatusCode = 200;
		//	response.StatusDescription = "OK";
		//	response.Close();
		//}


		//private void Error(int code, string description, HttpListenerResponse response)
		//{
		//	var errTpl = @"<s:Envelope xmlns:s=""http://schemas.xmlsoap.org/soap/envelope/"" 
		//							s:encodingStyle=""http://schemas.xmlsoap.org/soap/encoding/"">
		//			 <s:Body>
		//			  <s:Fault>
		//			   <faultcode>s:Client</faultcode>
		//			   <faultstring>UPnPError</faultstring>
		//			   <detail>
		//				<UPnPError xmlns=""urn:schemas-upnp-org:control-1-0"">
		//				 <errorCode>{0}</errorCode>
		//				 <errorDescription>{1}</errorDescription>
		//				</UPnPError>
		//			   </detail>
		//			  </s:Fault>
		//			 </s:Body>
		//			</s:Envelope>";
		//	var errorXml = string.Format(errTpl, code, description);
		//	var bytes = Encoding.UTF8.GetBytes(errorXml);
		//	response.OutputStream.Write(bytes, 0, bytes.Length);
		//	response.OutputStream.Flush();
		//	response.StatusCode = 500;
		//	response.StatusDescription = "Internal Server Error";
		//	response.Close();
		//}

		//private void ProcessGetExternalIpAddress(XElement envelop, HttpListenerResponse response)
		//{
		//	var responseXml = @"<?xml version=""1.0""?>
		//		<s:Envelope xmlns:s=""http://schemas.xmlsoap.org/soap/envelope/"" 
		//					s:encodingStyle=""http://schemas.xmlsoap.org/soap/encoding/"">
		//		  <s:Body>
		//			<m:GetExternalIPAddressResponse xmlns:m=""urn:schemas-upnp-org:service:" + _cfg.ServiceType + @""">
		//			  <NewExternalIPAddress>222.222.111.111</NewExternalIPAddress>
		//			</m:GetExternalIPAddressResponse>
		//		  </s:Body>
		//		</s:Envelope>";
		//	var bytes = Encoding.UTF8.GetBytes(responseXml);
		//	response.OutputStream.Write(bytes, 0, bytes.Length);
		//	response.OutputStream.Flush();
		//	response.StatusCode = 200;
		//	response.StatusDescription = "OK";
		//	response.Close();
		//}

		public void Dispose()
		{
			_listener.Close();
		}
	}

	static class HttpListenerResponseExtensions
	{
		public static void Status(this HttpListenerResponse res, int statusCode, string description)
		{
			res.StatusCode = statusCode;
			res.StatusDescription = description;
			res.Close();
		}
	}
}
