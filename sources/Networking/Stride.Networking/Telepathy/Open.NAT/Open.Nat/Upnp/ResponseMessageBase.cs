//
// Authors:
//   Lucas Ontivero lucas.ontivero@gmail.com
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
using System.Xml;

namespace Open.Nat
{
	internal abstract class ResponseMessageBase
	{
		private readonly XmlDocument _document;
		protected string ServiceType;
		private readonly string _typeName;

		protected ResponseMessageBase(XmlDocument response, string serviceType, string typeName)
		{
			_document = response;
			ServiceType = serviceType;
			_typeName = typeName;
		}

		protected XmlNode GetNode()
		{
			var nsm = new XmlNamespaceManager(_document.NameTable);
			nsm.AddNamespace("responseNs", ServiceType);

			string typeName = _typeName;
			string messageName = typeName.Substring(0, typeName.Length - "Message".Length);
			XmlNode node = _document.SelectSingleNode("//responseNs:" + messageName, nsm);
			if (node == null) throw new InvalidOperationException("The response is invalid: " + messageName);

			return node;
		}
	}
}