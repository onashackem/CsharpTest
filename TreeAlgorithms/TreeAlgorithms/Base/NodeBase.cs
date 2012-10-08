using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TreeAlgorithms.Base
{
    abstract class NodeBase <TKey, TValue>
    {
        /// <summary>
        /// Gets the key of the node
        /// </summary>
        public virtual TKey Key { get; protected set; }

        /// <summary>
        /// Gets the value contained in this node
        /// </summary>
        public virtual TValue Value { get; protected set; }

        /// <summary>
        /// Gets the parent node of this node
        /// </summary>
        public NodeBase<TKey, TValue> Parent { get; protected set; }

        /// <summary>
        /// Gets the array of children nodes of thos node
        /// </summary>
        public NodeBase<TKey, TValue>[] Children { get; protected set; }

        /// <summary>
        /// Gets total count of children
        /// </summary>
        public int ChildrenCount { get; protected set; }

        /// <summary>
        /// Gets the most left child
        /// </summary>
        public NodeBase<TKey, TValue> TopLeftChild
        {
            get
            {
                return GetChildAt(0);
            }

            set
            {
                AddChildAtIndex(value, 0);
            }
        }

        /// <summary>
        /// Gets the most right child
        /// </summary>
        public NodeBase<TKey, TValue> TopRightChild
        {
            get
            {
                return GetChildAt(ChildrenCount - 1);
            }

            set 
            {
                AddChildAtIndex(value, ChildrenCount - 1);
            }
        }

        /// <summary>
        /// Gets the depth of this node
        /// </summary>
        public int Depth
        {
            get
            {
                return Children.Max(child => (child == null) ? 0 : child.Depth) + 1;
            }
        }
        
        /// <summary>
        /// Contructor that inits the Children, Parent, Key and Value properties
        /// </summary>
        /// <param name="parent">Parent of the node</param>
        /// <param name="key">Key of the node</param>
        /// <param name="value">Value of the node</param>
        /// <param name="childCoutn">Size of children array</param>
        protected NodeBase(NodeBase<TKey, TValue> parent, TKey key, TValue value, int childCount)
        {
            // Set properties
            Parent = parent;
            Key = key;
            Value = value;
            ChildrenCount = childCount;
            Children = new NodeBase<TKey, TValue>[ChildrenCount];
        }

        /// <summary>
        /// Adds child to a specific index
        /// </summary>
        /// <param name="child">Child to add</param>
        /// <param name="index">Index to add chaild at</param>
        /// <param name="overwrite">If set to true, exisiting child is killed</param>
        /// <returns>Returns true if child added, false otherwise</returns>
        public bool AddChild(NodeBase<TKey, TValue> child, int index, bool overwrite = false)
        {
            if (ValidateChildIndex(index))
            {
                // Can't kill the child
                if (!overwrite && Children[index] != null)
                {
                    return false;
                }

                // Put the child on the index
                AddChildAtIndex(child, index);
            }

            return false;
        }

        /// <summary>
        /// Gets child from a specific index
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        public NodeBase<TKey, TValue> GetChildAt(int index)
        {
            if (ValidateChildIndex(index))
            {
                return Children[index];
            }

            return null;
        }

        /// <summary>
        /// Sets parent to this node
        /// </summary>
        /// <param name="parent"></param>
        public void SetParent(NodeBase<TKey, TValue> parent)
        {
            Parent = parent;
        }

        /// <summary>
        /// Asserts that 0 &lt;= index &lt; ChildrenCount to properly index the Children array
        /// </summary>
        /// <param name="index">Index to validate</param>
        /// <returns>Returns true if index is valid, otherwise an assert fails</returns>
        protected bool ValidateChildIndex(int index)
        {
            System.Diagnostics.Debug.Assert(index >= 0, "Index must be non-zero.");
            System.Diagnostics.Debug.Assert(index < ChildrenCount, "Index out of bounds.");

            return true;
        }

        /// <summary>
        /// Adds child to a specific index. If another child is at that index, is overwriten.
        /// </summary>
        /// <param name="child">Child to add</param>
        /// <param name="index">Index to add child at</param>
        protected void AddChildAtIndex(NodeBase<TKey, TValue> child, int index)
        {
            Children[index] = child;
            child.Parent = this;
        }
    }
}
