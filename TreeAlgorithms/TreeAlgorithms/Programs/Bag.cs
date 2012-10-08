using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TreeAlgorithms.Programs.Base;
using TreeAlgorithms.Concrete;

namespace TreeAlgorithms.Programs
{
    class Bag : IProgram
    {
        public void run(string[] args)
        {
            SimpleBinaryNode root = new SimpleBinaryNode(20);            
            SimpleIntBinaryTree tree = new SimpleIntBinaryTree(root);

            root.AddChild(new SimpleBinaryNode(34), 0);
            root.AddChild(new SimpleBinaryNode(22), 1);

            root.TopLeftChild.AddChild(new SimpleBinaryNode(12), 0);
            root.TopLeftChild.AddChild(new SimpleBinaryNode(13), 1);

            root.TopRightChild.AddChild(new SimpleBinaryNode(23), 0);
            root.TopRightChild.AddChild(new SimpleBinaryNode(11), 1);

            root.TopRightChild.TopRightChild.AddChild(new SimpleBinaryNode(8), 0);
            root.TopRightChild.TopRightChild.AddChild(new SimpleBinaryNode(4), 1);

            var minB = tree.MinB;
            Console.WriteLine("---");
            var minD_IN = tree.MinD_IN;
            Console.WriteLine("---");
            var minD_PRE = tree.MinD_PRE;
            Console.WriteLine("---");
            var minD_POST = tree.MinD_POST;
            Console.WriteLine("---");
        }
    }
}
