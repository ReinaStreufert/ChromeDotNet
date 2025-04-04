using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LibChromeDotNet.CDP
{
    // wrapper of List<T> made concurrent using locks
    class LockedList<T>
    {
        private List<T> _List = new List<T>();
        private object _Lock = new object();

        public DateTime Acquire(Action<List<T>> callback)
        {
            lock (_Lock)
            {
                var time = DateTime.Now;
                callback(_List);
                return time;
            }
        }

        public ListSnapshot<T> AcquireSnapshot(bool clearList = true)
        {
            lock (_Lock)
            {
                var time = DateTime.Now;
                var items = _List.ToArray();
                if (clearList)
                    _List.Clear();
                return new ListSnapshot<T>(time, items);
            }
        }
    }

    class ListSnapshot<T>
    {
        public DateTime Time { get; }
        public T[] Items { get; }

        public ListSnapshot(DateTime time, T[] items)
        {
            Time = time;
            Items = items;
        }
    }
}
