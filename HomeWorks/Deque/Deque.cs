using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Deque
{
    public class Deque<T> : IDeque<T>
    {
        private readonly double SHRINK_WHEN_CAPACITY = 0.4;
        private readonly double SHRINK_TO_CAPACITY = 0.6;
        private readonly double ENLARGE_WHEN_CAPACITY = 1;
        private readonly double ENLARGE_TO_CAPACITY = 2;
        private readonly int INITIAL_CAPACITY = 100;

        private T[] deque;
        private int firstIndex;
        private int lastIndex;        
        private HashSet<T> content = new HashSet<T>();

        public Deque()
            : this(-1)
        {
        }

        protected Deque(int initialCapacity)
        {
            if (initialCapacity < 0)
                initialCapacity = INITIAL_CAPACITY;

            Initialize(initialCapacity);
        }

        #region IDeque implementation

        public int Count { get; protected set; }

        public void Insert(int index, T item)
        {
            Insert(index, item, shiftCurrent: true);
        }

        public bool Remove(T item)
        {
            return RemoveFromBegining(item);
        }

        public void RemoveAt(int index)
        {
            RemoveFromBegining(index);
        }

        public bool RemoveFromBegining(T item)
        {
            // TODO
            return true;
        }

        public bool RemoveFromEnd(T item)
        {
            // TODO
            return true;
        }

        public void RemoveFromBegining(int index)
        {
            // TODO
        }

        public void RemoveFromEnd(int index)
        {
            // TODO:
        }

        public T this[int index]
        {
            get
            {
                return deque[index];
            }
            set
            {
                Insert(index, value, shiftCurrent: false);
            }
        }

        public void Add(T item)
        {
            AddFirst(item);
        }

        public void AddFirst(T item)
        {
            deque[firstIndex++] = item;
            ++Count;

            ReallocateIfNessesary();
        }

        public void AddLast(T item)
        {
            deque[lastIndex--] = item;
            ++Count;

            ReallocateIfNessesary();
        }

        public void Clear()
        {
            // TODO: check ArrayCopy

            // Clear cells, then reset pointers to first and last
            var emptyValue = default(T);
            for (int i = 0; i < firstIndex; ++i)
            {
                deque[i] = emptyValue;
            }

            for (int i = deque.Length - 1; i > lastIndex; --i)
            {
                deque[i] = emptyValue;
            }

            firstIndex = 0;
            lastIndex = deque.Length - 1;
        }

        public int IndexOf(T item)
        {
            if (Contains(item))
            {
                for(int i = firstIndex - 1; i > lastIndex; ++i)
                {
                    if (item.Equals(deque[i]))
                        return i;
                }
            }

            return -1;
        }

        public bool Contains(T item)
        {
            return content.Contains(item);
        }

        public void CopyTo(T[] array, int arrayIndex)
        {
            //Array.Copy c;
        }

        public bool IsReadOnly
        {
            get { return false; }
        }

        public IEnumerator<T> GetEnumerator()
        {
            //return new DequeEnumerator<T>(firstIndex, lastIndex, deque);
            for(int i = firstIndex - 1; i != lastIndex; --i)
            {
                yield return deque[i];

                if (i == 0)
                {
                    i = deque.Length;
                }
            }
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return GetEnumerator() as System.Collections.IEnumerator;
        }

        public IList<T> GetReversedView()
        {
            return new ReversedDeque<T>(this);
        }

        #endregion

        #region Protecetd members

        protected void Insert(int index, T item, bool shiftCurrent)
        {
            if (shiftCurrent)
            {
                //TODO:
            }

            deque[index] = item;
        }

        protected void Initialize(int initialCapacity)
        {
            deque = new T[initialCapacity];

            firstIndex = 0;
            lastIndex = initialCapacity - 1;

            Count = 0;
        }

        protected void ReallocateIfNessesary()
        {
            bool shrink;
            if (IsNessesaryToReallocate(out shrink))
            {
                Reallocate(shrink);
            }
        }

        protected void Reallocate(bool shrink)
        {
            // TODO: Cant enlarge to infinity
            int newCapacity = shrink ? (int)(deque.Length * SHRINK_TO_CAPACITY) : (int)(deque.Length * ENLARGE_TO_CAPACITY);
            Initialize(newCapacity);
        }

        protected bool IsNessesaryToReallocate(out bool shrink)
        {
            // Do not manipulate with the inititial deque
            if (Count < INITIAL_CAPACITY)
            {
                shrink = false;
                return false;
            }

            // Check for shrinking
            if (Count * SHRINK_WHEN_CAPACITY == deque.Length)
            {
                shrink = true;
                return true;
            }

            // TODO: Cant enlarge to infinity!
            // Check for enlargement
            shrink = false;
            return Count * ENLARGE_WHEN_CAPACITY == deque.Length;
        }

        #endregion
    }

    public class ReversedDeque<T> : IDeque<T>
    {
        IDeque<T> deque;

        public ReversedDeque(IDeque<T> deque)
        {
            this.deque = deque;
        }

        public IList<T> GetReversedView()
        {
            return new ReversedDeque<T>(this);
        }

        public int IndexOf(T item)
        {
            return deque.IndexOf(item);
        }

        public void Insert(int index, T item)
        {
            deque.Insert(index, item);
        }

        public void RemoveAt(int index)
        {
            deque.RemoveAt(index);
        }

        public T this[int index]
        {
            get
            {
                return deque[index];
            }
            set
            {
                deque[index] = value;
            }
        }

        public void Add(T item)
        {
            deque.AddLast(item);
        }

        public void Clear()
        {
            deque.Clear();
        }

        public bool Contains(T item)
        {
            return deque.Contains(item);
        }

        public void CopyTo(T[] array, int arrayIndex)
        {
            deque.CopyTo(array, arrayIndex);
        }

        public int Count
        {
            get { return deque.Count; }
        }

        public bool IsReadOnly
        {
            get { return deque.IsReadOnly; }
        }

        public bool Remove(T item)
        {
            return deque.Remove(item);
        }

        public IEnumerator<T> GetEnumerator()
        {
            return deque.GetEnumerator();
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return deque.GetEnumerator();
        }


        public void AddFirst(T item)
        {
            deque.AddLast(item);
        }

        public void AddLast(T item)
        {
            deque.AddFirst(item);
        }

        public bool RemoveFromBegining(T item)
        {
            return RemoveFromEnd(item);
        }

        public bool RemoveFromEnd(T item)
        {
            return RemoveFromBegining(item);
        }

        public void RemoveFromBegining(int index)
        {
            RemoveFromEnd(index);
        }

        public void RemoveFromEnd(int index)
        {
            RemoveFromBegining(index);
        }
    }

    public static class DequeTest
    {
        public static IList<T> GetReverseView<T>(Deque<T> d) {
            return d.GetReversedView();
	    }
    }

    public interface IDeque<T> : IList<T>
    {
        IList<T> GetReversedView();

        void AddFirst(T item);

        void AddLast(T item);

        bool RemoveFromBegining(T item);

        bool RemoveFromEnd(T item);

        void RemoveFromBegining(int index);

        void RemoveFromEnd(int index);
    }
}
