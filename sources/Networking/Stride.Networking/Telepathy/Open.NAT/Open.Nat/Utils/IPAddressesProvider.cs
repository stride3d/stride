//
// Authors:
//   Lucas Ontivero lucasontivero@gmail.com 
//
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
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;

namespace Open.Nat
{
	internal class IPAddressesProvider : IIPAddressesProvider
	{
		#region IIPAddressesProvider Members

		public IEnumerable<IPAddress> UnicastAddresses()
		{
			return IPAddresses(p => p.UnicastAddresses.Select(x => x.Address));
		}

		public IEnumerable<IPAddress> DnsAddresses()
		{
			return IPAddresses(p => p.DnsAddresses);
		}

		public IEnumerable<IPAddress> GatewayAddresses()
		{
			return IPAddresses(p => p.GatewayAddresses.Select(x => x.Address));
		}

		#endregion

		private static IEnumerable<IPAddress> IPAddresses(Func<IPInterfaceProperties, IEnumerable<IPAddress>> ipExtractor)
		{
			return from networkInterface in NetworkInterface.GetAllNetworkInterfaces()
				   where
					   networkInterface.OperationalStatus == OperationalStatus.Up ||
					   networkInterface.OperationalStatus == OperationalStatus.Unknown
				   let properties = networkInterface.GetIPProperties()
				   from address in ipExtractor(properties)
				   where address.AddressFamily == AddressFamily.InterNetwork 
				      || address.AddressFamily == AddressFamily.InterNetworkV6
				   select address;
		}
	}
}