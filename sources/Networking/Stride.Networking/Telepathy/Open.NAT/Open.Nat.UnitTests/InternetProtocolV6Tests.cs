using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Open.Nat.Tests
{
	[TestClass]
	public class InternetProtocolV6Tests
	{
		private UpnpMockServer _server;
		private ServerConfiguration _cfg;

		[TestInitialize]
		public void Setup()
		{
			_cfg = new ServerConfiguration();
			_cfg.Prefix = "http://*:5431/";
			_cfg.ServiceUrl = "http://[::1]:5431/dyndev/uuid:0000e068-20a0-00e0-20a0-48a8000808e0";
			_cfg.ControlUrl = "http://[::1]:5431/uuid:0000e068-20a0-00e0-20a0-48a802086048/WANIPConnection:1";
			_server = new UpnpMockServer(_cfg);
			_server.Start();
		}

		[TestCleanup]
		public void TearDown()
		{
			_server.Dispose();
		}

		[TestMethod]
#if NET35
		public void Connect()
#else
		public async Task Connect()
#endif
		{
			_server.WhenDiscoveryRequest = () =>
					  "HTTP/1.1 200 OK\r\n"
					+ "Server: Custom/1.0 UPnP/1.0 Proc/Ver\r\n"
					+ "EXT:\r\n"
					+ "Location: http://[::1]:5431/dyndev/uuid:0000e068-20a0-00e0-20a0-48a8000808e0\r\n"
					+ "Cache-Control:max-age=1800\r\n"
					+ "ST:urn:schemas-upnp-org:service:WANIPConnection:1\r\n"
					+ "USN:uuid:0000e068-20a0-00e0-20a0-48a802086048::urn:schemas-upnp-org:service:WANIPConnection:1";

			_server.WhenGetExternalIpAddress = (ctx) =>
			{
				var responseXml = "<?xml version=\"1.0\"?>" +
					"<s:Envelope xmlns:s=\"http://schemas.xmlsoap.org/soap/envelope/\" " +
					"s:encodingStyle=\"http://schemas.xmlsoap.org/soap/encoding/\">" +
					"<s:Body>" +
					"<m:GetExternalIPAddressResponse xmlns:m=\"urn:schemas-upnp-org:service:WANIPConnection:1\">" +
					"<NewExternalIPAddress>FE80::0202:B3FF:FE1E:8329</NewExternalIPAddress>" +
					"</m:GetExternalIPAddressResponse>" +
					"</s:Body>" +
					"</s:Envelope>";
				var bytes = Encoding.UTF8.GetBytes(responseXml);
				var response = ctx.Response;
				response.OutputStream.Write(bytes, 0, bytes.Length);
				response.OutputStream.Flush();
				response.StatusCode = 200;
				response.StatusDescription = "OK";
				response.Close();
			};

			var nat = new NatDiscoverer();
#if NET35
			var cts = new CancellationTokenSource();
			cts.CancelAfter(5000);

			NatDevice device =null;
			nat.DiscoverDeviceAsync(PortMapper.Upnp, cts)
			.ContinueWith(tt =>
			{
				device = tt.Result;
				Assert.IsNotNull(device);
			});

			device.GetExternalIPAsync()
			.ContinueWith(tt =>
			{
				var ip = tt.Result;
				Assert.AreEqual(IPAddress.Parse("FE80::0202:B3FF:FE1E:8329"), ip);
			});

#else
			var cts = new CancellationTokenSource(5000);
			var device = await nat.DiscoverDeviceAsync(PortMapper.Upnp, cts);
			Assert.IsNotNull(device);

			var ip = await device.GetExternalIPAsync();
			Assert.AreEqual(IPAddress.Parse("FE80::0202:B3FF:FE1E:8329"), ip);
#endif
		}
	}
}
