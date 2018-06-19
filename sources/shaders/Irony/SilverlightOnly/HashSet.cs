using System.Linq;

namespace System.Collections.Generic {
  /// <summary>
  /// This is a minimal implementation of the missing HashSet from Silverlight BCL
  /// It's nowhere near the real one, it's just enough to make Irony work with Silverlight
  /// </summary>
  public class HashSet<T> : Dictionary<T, bool>, IEnumerable<T> {
    public HashSet() {

    }

    public HashSet(StringComparer comparer)
      : base((IEqualityComparer<T>)comparer) {

    }

    public void UnionWith(IEnumerable<T> items) {
      foreach (var item in items) {
        Add(item);
      }
    }

    public void IntersectWith(HashSet<T> items) {
      List<T> removal = new List<T>();
      foreach (var item in this) {
        if (!items.Contains(item)) {
          removal.Add(item);
        }
      }
      foreach (var item in removal) {
        Remove(item);
      }
    }

    public bool Overlaps(HashSet<T> items) {
      return this.Where<T>(x => items.Contains(x)).Count() > 0;
    }

    public void ExceptWith(HashSet<T> items) {
      List<T> removal = new List<T>();
      foreach (var item in this) {
        if (items.Contains(item)) {
          removal.Add(item);
        }
      }
      foreach (var item in removal) {
        Remove(item);
      }
    }

    public bool Contains(T item) {
      return ContainsKey(item);
    }

    public T First() {
      return Keys.First();
    }

    public void RemoveWhere(Func<T, bool> predicate) {
      var removal = this.Where<T>(predicate);
      foreach (var item in removal) {
        Remove(item);
      }
    }

    public bool Add(T item) {
      if (Contains(item)) {
        return false;
      }
      Add(item, true);
      return true;
    }

    public T this[int index] {
      get { throw new NotImplementedException(); }
    }

    public new IEnumerator<T> GetEnumerator() {
      return this.Keys.GetEnumerator();
    }

    public T[] ToArray() {
      return this.Keys.ToArray();
    }
  }
}
