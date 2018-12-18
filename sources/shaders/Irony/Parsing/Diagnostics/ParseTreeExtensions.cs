#region License
/* **********************************************************************************
 * Copyright (c) Roman Ivantsov
 * This source code is subject to terms and conditions of the MIT License
 * for Irony. A copy of the license can be found in the License.txt file
 * at the root of this distribution. 
 * By using this source code in any fashion, you are agreeing to be bound by the terms of the 
 * MIT License.
 * You must not remove this notice from this software.
 * **********************************************************************************/
#endregion

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO; 
using System.Xml;

namespace Irony.Parsing { 
  public static class ParseTreeExtensions {

    public static string ToXml(this ParseTree parseTree) {
      if (parseTree == null || parseTree.Root == null) return string.Empty;
      var xdoc = ToXmlDocument(parseTree); 
      StringWriter sw = new StringWriter();
      XmlWriterSettings xs = new XmlWriterSettings();
      xs.Indent = true;
      XmlWriter xw = XmlWriter.Create(sw, xs);
      xdoc.WriteTo(xw);
      xw.Flush();
      return sw.ToString();
    }
    
    public static XmlDocument ToXmlDocument(this ParseTree parseTree) {
      var xdoc = new XmlDocument();
      if (parseTree == null || parseTree.Root == null) return xdoc;
      var xTree = xdoc.CreateElement("ParseTree");
      xdoc.AppendChild(xTree); 
      var xRoot = parseTree.Root.ToXmlElement(xdoc);
      xTree.AppendChild(xRoot);
      return xdoc; 
    }

    public static XmlElement ToXmlElement(this ParseTreeNode node, XmlDocument ownerDocument) {
      var xElem = ownerDocument.CreateElement("Node");
      xElem.SetAttribute("Term", node.Term.Name);
      if (node.Term.AstNodeType != null) 
        xElem.SetAttribute("AstNodeType", node.Term.AstNodeType.Name);
      if (node.Token != null) {
        xElem.SetAttribute("Terminal", node.Term.GetType().Name);
        //xElem.SetAttribute("Text", node.Token.Text);
        if (node.Token.Value != null)
          xElem.SetAttribute("Value", node.Token.Value.ToString()); 
      } else 
        foreach (var child in node.ChildNodes) {
          var xChild = child.ToXmlElement(ownerDocument);
          xElem.AppendChild(xChild); 
        }
      return xElem;
    }//method

  }//class
}//namespace
