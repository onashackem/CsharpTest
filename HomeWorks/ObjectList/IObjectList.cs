using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ObjectList
{
    /// <summary>
    /// Thread safe list of objects with maximal performance and minimal memory requirements 
    /// when 0 or 1 objects are in the list
    /// </summary> 
    interface IObjectList
    {
        void AddObject(object o);

        object GetFirstObject();

        object RemoveFirstObject();
    }
}
