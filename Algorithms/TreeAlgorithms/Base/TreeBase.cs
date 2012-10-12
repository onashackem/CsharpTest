using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
namespace TreeAlgorithms.Base
{
    abstract class TreeBase <TKey, TValue> : ITree<TKey,TValue>
    {
        /// <summary>
        /// Gets the root of the tree
        /// </summary>
        public INodeBase<TKey, TValue> Root { get; protected set; }

        /// <summary>
        /// Gets depth of a tree
        /// </summary>
        public int Depth
        {
            get
            {
                return Root == null
                    ? 0
                    : CountNodeDepth(Root);
            }
        }

        /// <summary>
        /// Gets total count of nodes in the tree
        /// </summary>
        public int NodeCount
        {
            get
            {
                return Root == null
                    ? 0
                    : Dfs<int>((node, x) => x + 1, 1, Base.BfsOrder.POST_ORDER);
            }
        }

        /// <summary>
        /// Constructor that initializes root of the tree
        /// </summary>
        /// <param name="root"></param>
        protected TreeBase(INodeBase<TKey, TValue> root)
        {
            Root = root;
        }

        /// <summary>
        /// Constructor with no root
        /// </summary>
        public TreeBase()
            : this(null)
        {
            
        }

        /// <summary>
        /// Breath-first-search
        /// </summary>
        /// <param name="predicate">Function that accepst Node and result. 
        /// The result of a predicate is an result of aggregated operation with supplied node and supplied result.</param>
        /// <param name="initialValue">Initial value for the first predicate operation</param>
        /// <returns>Returns agregated result of DFS</returns>
        public TResult Bfs<TResult>(Func<INodeBase<TKey, TValue>, TResult, TResult> predicate, TResult initialValue)
        {
            // Nothing to search in
            if (Root == null)
                return default(TResult);

            // Initialize queue with a
            LinkedList<INodeBase<TKey, TValue>> queue = new LinkedList<INodeBase<TKey, TValue>>();
            queue.AddLast(Root);
            
            // Initial result
            TResult result = initialValue;
            while (queue.Count > 0)
            {
                // Current node
                var node = queue.First.Value;
                queue.RemoveFirst();

                // Add children to the end of queue
                foreach (var child in node.Children)
                {
                    // Nothing to do
                    if (child == null)
                        continue;

                    queue.AddLast(child);
                }

                // Apply predicate to node
                result = predicate(node, result);
            }

            return result;
        }

        /// <summary>
        /// Depth-first-search
        /// </summary>
        /// <param name="predicate">Function that accepst Node and result. 
        /// The result of a predicate is an result of aggregated operation with supplied node and supplied result.</param>
        /// <param name="initialValue">Partial result for the predicate operation</param>
        /// <returns>Returns agregated result of DFS</returns>
        public TResult Dfs<TResult>(Func<INodeBase<TKey, TValue>, TResult, TResult> predicate, TResult initialValue, BfsOrder order)
        {
            TResult result = initialValue;

            // Nothing to search in
            if (Root == null)
                return default(TResult);

            // Apply recursion from the root
            DfsVisitChild(Root, predicate, ref result, order);

            return result;
        }

        protected TResult DfsVisitChild<TResult>(INodeBase<TKey, TValue> node, Func<INodeBase<TKey, TValue>, TResult, TResult> predicate, ref TResult result, BfsOrder order)
        {

            // Apply prediace in pre-order
            if (order == BfsOrder.PRE_ORDER)
                result = predicate(node, result);

            // Add children to the end of queue
            foreach (var child in node.Children)
            {
                // Nothing to do
                if (child == null)
                    continue;

                DfsVisitChild(child, predicate, ref result, order);

                // Apply predicate between two nodes
                if (order == BfsOrder.IN_ORDER)
                    result = predicate(node, result);
            }

            // Apply prediace in post-order
            if (order == BfsOrder.POST_ORDER)
                result = predicate(node, result);

            return result;
        }

        /// <summary>
        /// Counts depth from bottom of the current node
        /// </summary>
        /// <param name="node">Node to count depth for</param>
        /// <returns></returns>
        private int CountNodeDepth(INodeBase<TKey, TValue> node)
        {
            var maxDepth = 0;
            foreach (var child in node.Children)
            {
                var depth = child == null ? 0 : CountNodeDepth(child);

                if (depth > maxDepth)
                    maxDepth = depth;
            }

            return ++maxDepth;
        }
    }

    public enum BfsOrder
    {
        PRE_ORDER, 

        IN_ORDER,

        POST_ORDER
    };
}
