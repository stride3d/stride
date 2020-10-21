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
using System.Globalization;
using System.Linq;

namespace Open.Nat
{
	class DiscoveryResponseMessage
	{
		private readonly IDictionary<string, string> _headers;

		public DiscoveryResponseMessage(string message)
		{
			var lines = message.Split(new[]{"\r\n"}, StringSplitOptions.RemoveEmptyEntries);
			var headers = from h in lines.Skip(1)
					let c = h.Split(':')
					let key = c[0]
					let value = c.Length > 1 
						? string.Join(":", c.Skip(1).ToArray()) 
						: string.Empty 
					select new {Key = key, Value = value.Trim()};
			_headers = headers.ToDictionary(x => x.Key.ToUpperInvariant(), x => x.Value);
		}

		public string this[string key]
		{
			get { return _headers[key.ToUpperInvariant()]; }
		}
	}
}
