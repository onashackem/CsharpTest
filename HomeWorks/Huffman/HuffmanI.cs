using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Collections;

namespace Huffman
{
    class HuffmanI
    {
        static void Main(string[] args)
        {
            if (args.Length != 1)
            {
                Console.WriteLine("Argument Error");
                return;
            }

            string INTPUT_FILE = args[0];

            // Dictionary cotains (read char, its cardinality) pairs
            Dictionary<int, long> cardinialities = new Dictionary<int, long>();

            try
            {
                // Read cardinalities
                using(FileStream reader = new FileStream(INTPUT_FILE, FileMode.Open))
                {
                    int character;
                    while ((character = reader.ReadByte()) > -1)
                    {                         
                        if (!cardinialities.ContainsKey(character))
                            cardinialities.Add(character, 0);

                        ++cardinialities[character];
                    }
                }

                // Trees sorted ascending by weight
                SortedList<Tree, long> sortedTrees 
                    = new SortedList<Tree, long>(new SortTreesAscending());

                // Fill the previous dictionary
                foreach(var key in cardinialities.Keys)
                {
                    var tree = new Tree(new Node(key, cardinialities[key]));
                    sortedTrees.Add(tree, tree.Weight);
                }
                
                // Free memory
                cardinialities = null;

                // Take 2 trees, add one
                while (sortedTrees.Count > 1)
                {
                    // Take first
                    var first = sortedTrees.First().Key;
                    sortedTrees.RemoveAt(0);

                    // Take second
                    var second = sortedTrees.First().Key;
                    sortedTrees.RemoveAt(0);

                    var newTree = new Tree(first, second);
                    sortedTrees.Add(newTree, newTree.Weight);
                }

                sortedTrees.ElementAt(0).Key.PrintTree(System.Console.Out);
            }
            catch (Exception)
            {
                Console.WriteLine("File Error");
            }
        }

        class SortTreesAscending : IComparer<Tree>
        {
            public int Compare(Tree x, Tree y)
            {
                // Compare weights
                if (x.Weight != y.Weight)
                    return (x.Weight < y.Weight) ? -1 : 1;

                // Compare characters in both Trees have only roots
                if (x.Root != null && y.Root != null)
                    return (x.Root.Character < y.Root.Character) ? -1 : 1;

                // One is LeafNode
                if (x.Root == null || y.Root == null)
                    return (x.Root == null) ? -1 : 1;

                // TODO: Can't be same
                return 0;
            }
        }

        class Tree
        {
            public Node Root { get; private set; }

            public Tree Left { get; private set; }

            public Tree Right { get; private set; }

            public long Weight { get; private set; }

            public Tree(Node root)
            {
                Root = root;
                Left = null;
                Right = null;

                Weight = Root.Weight;
            }

            public Tree(Tree left, Tree right)
            {
                Root = null;

                // Lighter to the left
                if (left.IsLighterThan(right))
                {
                    Left = left;
                    Right = right;
                }
                else
                {
                    Left = right;
                    Right = left;
                }

                Weight = Left.Weight + Right.Weight;
            }

            public void PrintTree(TextWriter textWriter)
            {
                if (Root == null)
                {
                    textWriter.Write(Weight);
                    textWriter.Write(" ");
                    Left.PrintTree(textWriter);
                    textWriter.Write(" ");
                    Right.PrintTree(textWriter);
                }
                else
                {
                    textWriter.Write(String.Format("*{0}:{1}", Root.Character, Root.Weight));
                }
            }

            private bool IsLighterThan(Tree otherTree)
            {
                // Compare weights
                if (Weight != otherTree.Weight)
                    return Weight < otherTree.Weight;

                // Compare characters in both Trees have only roots
                if (this.Root != null && otherTree.Root != null)
                    return this.Root.Character < otherTree.Root.Character;

                if (this.Root == null && otherTree.Root != null)
                    return false;

                // TODO:
                //if (this.Root != null && otherTree.Root == null)
                return true;
            }
        }

        class Node
        {
            public int Character { get; private set; }

            public long Weight { get; private set; }

            public Node(int character, long weight)
            {
                Character = character;
                Weight = weight;
            }
        }
    }
}
