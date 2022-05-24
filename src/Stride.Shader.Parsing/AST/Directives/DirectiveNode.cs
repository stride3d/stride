using Eto.Parse;
using Stride.Shader.Parsing.Grammars.Directive;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Stride.Shader.Parsing.AST.Directives;

public abstract class DirectiveToken { };

public class CodeSnippet : DirectiveToken
{
    public string Snippet { get; set; }
}

public class Define : DirectiveToken
{
    public string Variable { get; set; }
    public int Value { get; set; }
}

public class Shader : DirectiveToken, IList<DirectiveToken>, ICollection<DirectiveToken>
{
    List<DirectiveToken> nodes = new List<DirectiveToken>();


    #region list & collection impl

    public DirectiveToken this[int index] { get => nodes[index]; set{ nodes[index] = value; } }

    public int Count => nodes.Count;

    public bool IsReadOnly => ((ICollection<DirectiveToken>)nodes).IsReadOnly;

    public void Add(DirectiveToken item)
    {
        nodes.Add(item);
    }

    public void Clear()
    {
        nodes.Clear();
    }

    public bool Contains(DirectiveToken item)
    {
        return nodes.Contains(item);
    }

    public void CopyTo(DirectiveToken[] array, int arrayIndex)
    {
        nodes.CopyTo(array,arrayIndex);
    }

    public IEnumerator<DirectiveToken> GetEnumerator()
    {
       return nodes.GetEnumerator();
    }

    public int IndexOf(DirectiveToken item)
    {
        return nodes.IndexOf(item);
    }

    public void Insert(int index, DirectiveToken item)
    {
        nodes.Insert(index,item);
    }

    public bool Remove(DirectiveToken item)
    {
        return nodes.Remove(item);
    }

    public void RemoveAt(int index)
    {
        nodes.RemoveAt(index);
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        throw new NotImplementedException();
    }
    #endregion
}
