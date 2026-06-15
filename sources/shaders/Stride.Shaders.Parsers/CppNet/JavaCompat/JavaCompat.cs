using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

//using Set = System.Collections.Generic.HashSet;
//using ArrayList = System.Collections.Generic.List;
//using Map = System.Collections.Generic.Dictionary;

namespace CppNet
{
    static class JavaCompat
    {
        public static StringBuilder append(this StringBuilder bld, object value)
        {
            return bld.Append(value);
        }

        public static int length(this string str)
        {
            return str.Length;
        }

        public static char charAt(this string str, int i)
        {
            return str[i];
        }

        public static T get<T>(this List<T> list, int i)
        {
            return list[i];
        }

        public static Iterator<T> iterator<T>(this List<T> list)
        {
            return new ListIterator<T>(list);
        }

        public static string toString(this object o)
        {
            return o?.ToString() ?? "";
        }
    }

    class ListIterator<T> : Iterator<T>
    {
        List<T> _list;
        int _index;

        public ListIterator(List<T> list)
        {
            _list = list;
        }

        public bool hasNext()
        {
            return _index < _list.Count;
        }

        public T next()
        {
            return _list[_index++];
        }

        public void remove()
        {
            throw new NotImplementedException();
        }
    }

    internal interface Closeable
    {
        void close();
    }

    internal interface Iterable<T>
    {
        Iterator<T> iterator();
    }

    internal interface Iterator<T>
    {
        bool hasNext();
        T next();
        void remove();
    }

    internal class IllegalStateException : Exception
    {
        public IllegalStateException(Exception ex) : base("Illegal State", ex) { }
    }


}
