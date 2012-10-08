using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
namespace TreeAlgorithms.Base
{
    abstract class TreeBase <TKey, TValue>
    {
        /// <summary>
        /// Gets the root of the tree
        /// </summary>
        public NodeBase<TKey, TValue> Root { get; protected set; }

        public int Depth
        {
            get
            {
                return (Root == null) ? 0 : Root.Depth;
            }
        }

        /// <summary>
        /// Constructor that initializes root of the tree
        /// </summary>
        /// <param name="root"></param>
        protected TreeBase(NodeBase<TKey, TValue> root)
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
        public TResult Bfs<TResult>(Func<NodeBase<TKey, TValue>, TResult, TResult> predicate, TResult initialValue)
        {
            // Nothing to search in
            if (Root == null)
                return default(TResult);

            // Initialize queue with a
            LinkedList<NodeBase<TKey, TValue>> queue = new LinkedList<NodeBase<TKey, TValue>>();
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
        /// <param name="result">Partial result for the predicate operation</param>
        /// <returns>Returns agregated result of DFS</returns>
        public TResult Dfs<TResult>(Func<NodeBase<TKey, TValue>, TResult, TResult> predicate, ref TResult result, BfsOrder order)
        {
            // Nothing to search in
            if (Root == null)
                return default(TResult);

            // Apply recursion from the root
            return DfsVisitChild(Root, predicate, ref result, order);
        }

        protected TResult DfsVisitChild<TResult>(NodeBase<TKey, TValue> node, Func<NodeBase<TKey, TValue>, TResult, TResult> predicate, ref TResult result, BfsOrder order)
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

            Console.WriteLine(String.Format("{0}:{1}", node.Value, result));

            return result;
        }
    }

    public enum BfsOrder
    {
        PRE_ORDER, 

        IN_ORDER,

        POST_ORDER
    };
}
