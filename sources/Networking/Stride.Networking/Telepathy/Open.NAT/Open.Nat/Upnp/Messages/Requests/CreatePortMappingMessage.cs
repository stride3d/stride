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

using System.Collections.Generic;
using System.Net;

namespace Open.Nat
{
	internal class CreatePortMappingRequestMessage : RequestMessageBase
	{
		private readonly Mapping _mapping;

		public CreatePortMappingRequestMessage(Mapping mapping)
		{
			_mapping = mapping;
		}

		public override IDictionary<string, object> ToXml()
		{
			string remoteHost = _mapping.PublicIP.Equals(IPAddress.None)
									? string.Empty
									: _mapping.PublicIP.ToString();

			return new Dictionary<string, object>
					   {
						   {"NewRemoteHost", remoteHost},
						   {"NewExternalPort", _mapping.PublicPort},
						   {"NewProtocol", _mapping.Protocol == Protocol.Tcp ? "TCP" : "UDP"},
						   {"NewInternalPort", _mapping.PrivatePort},
						   {"NewInternalClient", _mapping.PrivateIP},
						   {"NewEnabled", 1},
						   {"NewPortMappingDescription", _mapping.Description},
						   {"NewLeaseDuration", _mapping.Lifetime}
					   };
		}
	}
}