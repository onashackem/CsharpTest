using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TreeAlgorithms.Base;

namespace TreeAlgorithms.Concrete
{
    /// <summary>
    /// Binary tree
    /// </summary>
    /// <typeparam name="TKey">Type of keys of nodes</typeparam>
    /// <typeparam name="TValue">Type of keys of nodes</typeparam>
    class BinaryTree<TKey, TValue> : TreeBase<TKey, TValue>
    {
        public BinaryTree()
            : this(null)
        {
        }

        public BinaryTree(BinaryNode<TKey, TValue> root)
            : base(root)
        {
        }

        /// <summary>
        /// Creates new node without parent
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public BinaryNode<TKey, TValue> createNode(TKey key, TValue value)
        {
            return new BinaryNode<TKey, TValue>(key, value);
        }
    }

    /// <summary>
    /// Binary node - has 2 children
    /// </summary>
    /// <typeparam name="TKey">Type of keys of the node</typeparam>
    /// <typeparam name="TValue">Type of values of the node</typeparam>
    class BinaryNode<TKey, TValue> : NodeBase<TKey, TValue>
    {
        /// <summary>
        /// Constructor for binary node
        /// </summary>
        /// <param name="parent">Parent of the node</param>
        /// <param name="key">Key of the node</param>
        /// <param name="value">Value of the node</param>
        public BinaryNode(BinaryNode<TKey, TValue> parent, TKey key, TValue value)
            : base(parent, key, value, 2)
        {

        }

        /// <summary>
        /// Constructor for binary node without parent
        /// </summary>
        /// <param name="key">Key of the node</param>
        /// <param name="value">Value of the node</param>
        public BinaryNode(TKey key, TValue value)
            : this(null, key, value)
        {

        }
    }
}
