using System;
namespace TreeAlgorithms.Base
{
    interface INodeBase<TKey, TValue>
    {
        int ChildrenCount { get; }

        TKey Key { get; }
        TValue Value { get; }

        INodeBase<TKey, TValue>[] Children { get; }
        INodeBase<TKey, TValue> Parent { get; }
        INodeBase<TKey, TValue> TopLeftChild { get; }
        INodeBase<TKey, TValue> TopRightChild { get; }

        bool AddChild(INodeBase<TKey, TValue> child, int index, bool overwrite = false);
        void SetParent(INodeBase<TKey, TValue> parent);
    }
}
