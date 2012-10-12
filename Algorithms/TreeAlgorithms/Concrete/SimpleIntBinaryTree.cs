using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TreeAlgorithms.Concrete
{
    class SimpleIntBinaryTree : BinaryTree<int, int>
    {
        public SimpleIntBinaryTree()
            : this(null)
        {
        }

        public SimpleIntBinaryTree(SimpleBinaryNode root)
            : base(root)
        {
        }

        public int MinB
        {
            get
            {                
                return Root == null 
                    ? -1 
                    : Bfs<int>((node, min) => (node.Value < min) ? node.Value : min, Root.Value); 
            }
        }

        public int MinD_IN
        {
            get
            {
                return Root == null 
                    ? -1 
                    : Dfs<int>((node, min) => (node.Value < min) ? node.Value : min, Root.Value, Base.BfsOrder.IN_ORDER);
            }
        }

        public int MinD_PRE
        {
            get
            {
                return Root == null 
                    ? -1
                    : Dfs<int>((node, min) => (node.Value < min) ? node.Value : min, Root.Value, Base.BfsOrder.PRE_ORDER);
            }
        }

        public int MinD_POST
        {
            get
            {
                return Root == null 
                    ? -1
                    : Dfs<int>((node, min) => (node.Value < min) ? node.Value : min, Root.Value, Base.BfsOrder.POST_ORDER);
            }
        }


    }

    class SimpleBinaryNode : BinaryNode<int, int>
    {
        /// <summary>
        /// Value is the same as Key
        /// </summary>
        public override int Value
        {
            get
            {
                return Key;
            }

            protected set
            {
                Key = value;
            }
        }

        public SimpleBinaryNode(SimpleBinaryNode parent, int key)
            : base(parent, key, key)
        {
        }

        public SimpleBinaryNode(int key)
            : this(null, key)
        {
        }


    }
}
