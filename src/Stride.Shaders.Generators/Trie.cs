using System.Reflection.Metadata;

namespace Stride.Shaders.Generators;

public class TrieNode<TValue>
{
    public TrieNode() { }
    public TrieNode(TrieNode<TValue> parent, string name)
    {
        Parent = parent;
    }

    public TrieNode<TValue>? Parent { get; init; }
    
    // Dictionary to hold children, keyed by character
    public Dictionary<string, TrieNode<TValue>> Children { get; } = new();

    // Optional: Store a value at this node
    public List<TValue> Values { get; } = new();
}

public class Trie<TValue> where TValue : class
{
    private readonly TrieNode<TValue> root = new();

    public TrieNode<TValue> Insert(List<string> keys, TValue value)
    {
        var node = root;
        foreach (string s in keys)
        {
            if (!node.Children.ContainsKey(s))
                node.Children[s] = new TrieNode<TValue>(node, s);
            node = node.Children[s];
        }

        node.Values.Add(value);
        return node;
    }
    
    public void SimplifySingleLeaves() => SimplifySingleLeaves(root);
    
    public IEnumerable<TrieNode<TValue>> EnumerateNodes() => EnumerateNodes(root);

    // Try to attach method definition to a parent definition with optional parameter (only if one option)
    // i.e. (a,b) and (a,b,c) will be grouped into (a,b,c?)
    // however (a,b) (a,b,c) and (a,b,d) won't be merged as (a,b) has two possible optional parameter branches
    private void SimplifySingleLeaves(TrieNode<TValue> node)
    {
        // First we recurse
        foreach (var child in node.Children.Values)
        {
            SimplifySingleLeaves(child);
        }
        
        // Check if we can merge node with its child
        if (node.Children.Count == 1 && node.Children.First().Value.Children.Count == 0)
        {
            var child = node.Children.First().Value;
            node.Children.Clear();
            // Take over child values
            node.Values.AddRange(child.Values);
        }
    }
    
    private IEnumerable<TrieNode<TValue>> EnumerateNodes(TrieNode<TValue> node)
    {
        // Return the current node itself
        yield return node;

        // Recursively return all nodes in the child subtrees
        foreach (var child in node.Children)
        {
            foreach (var node2 in EnumerateNodes(child.Value))
            {
                yield return node2;
            }
        }
    }
}