//
// Authors:
//   Alan McGovern  alan.mcgovern@gmail.com
//   Lucas Ontivero lucas.ontivero@gmail.com
//
// Copyright (C) 2006 Alan McGovern
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
using System.Xml;

namespace Open.Nat
{
	internal class GetPortMappingEntryResponseMessage : ResponseMessageBase
	{
		internal GetPortMappingEntryResponseMessage(XmlDocument response, string serviceType, bool genericMapping)
			: base(response, serviceType, genericMapping ? "GetGenericPortMappingEntryResponseMessage" : "GetSpecificPortMappingEntryResponseMessage")
		{
			XmlNode data = GetNode();

			RemoteHost = (genericMapping) ? data.GetXmlElementText("NewRemoteHost") : string.Empty;
			ExternalPort = (genericMapping) ? Convert.ToInt32(data.GetXmlElementText("NewExternalPort")) : ushort.MaxValue;
			if (genericMapping)
				Protocol = data.GetXmlElementText("NewProtocol").Equals("TCP", StringComparison.InvariantCultureIgnoreCase)
							   ? Protocol.Tcp
							   : Protocol.Udp;
			else
				Protocol = Protocol.Udp;

			InternalPort = Convert.ToInt32(data.GetXmlElementText("NewInternalPort"));
			InternalClient = data.GetXmlElementText("NewInternalClient");
			Enabled = data.GetXmlElementText("NewEnabled") == "1";
			PortMappingDescription = data.GetXmlElementText("NewPortMappingDescription");
			LeaseDuration = Convert.ToInt32(data.GetXmlElementText("NewLeaseDuration"));
		}

		public string RemoteHost { get; private set; }
		public int ExternalPort { get; private set; }
		public Protocol Protocol { get; private set; }
		public int InternalPort { get; private set; }
		public string InternalClient { get; private set; }
		public bool Enabled { get; private set; }
		public string PortMappingDescription { get; private set; }
		public int LeaseDuration { get; private set; }
	}
}