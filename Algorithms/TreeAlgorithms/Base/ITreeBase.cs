using System;
namespace TreeAlgorithms.Base
{
    interface ITree<TKey, TValue>
    {
        int Depth { get; }
        int NodeCount { get; }
        INodeBase<TKey, TValue> Root { get; }

        TResult Bfs<TResult>(Func<INodeBase<TKey, TValue>, TResult, TResult> predicate, TResult initialValue);
        TResult Dfs<TResult>(Func<INodeBase<TKey, TValue>, TResult, TResult> predicate, TResult initialValue, BfsOrder order);
    }
}
