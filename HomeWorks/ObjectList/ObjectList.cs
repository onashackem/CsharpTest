using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ObjectList
{
    class ObjectList : IObjectList
    {
        IObjectList innerRepresentation = new SingleObjectList();

        void IObjectList.AddObject(object o)
        {

        }

        object IObjectList.GetFirstObject()
        {
            return null;
        }

        object IObjectList.RemoveFirstObject()
        {
            return null;
        }

        private class SingleObjectList : IObjectList
        {
            private object value;

            public bool Empty { get { return value == null; } }

            public SingleObjectList()
            {
                value = null;
            }

            public SingleObjectList(MultiObjectList list)
            {
                value = list.RemoveFirstObject();
            }

            public void AddObject(object o)
            {
                if (value != null)
                    throw new InvalidOperationException("Cannot add second object");

                value = o;
            } 

            public object GetFirstObject()
            {
                return value;
            }

            public object RemoveFirstObject()
            {
                if (value == null)
                    throw new InvalidOperationException("Cannot remove from empty list");
                
                // Set value to null AFTER it is returned
                try
                {
                    return value;
                }
                finally
                {
                    value = null;
                }
            }
        }

        private class MultiObjectList : IObjectList
        {
            private List<object> objects = new List<object>();

            public MultiObjectList(SingleObjectList first)
            {
                objects.Add(first.GetFirstObject());
            }

            public void AddObject(object o)
            {
                lock (objects)
                {
                    objects.Add(o);
                }
            }

            public object GetFirstObject()
            {
                lock (objects)
                {
                    return objects.First();
                }
            }

            public object RemoveFirstObject()
            {
                lock (objects)
                {
                    object first = objects.First();
                    objects.RemoveAt(0);
                    return first;
                }
            }
        }
    }
}
