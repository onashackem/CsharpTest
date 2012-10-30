using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Collections;

namespace Huffman
{
    class Program
    {
        static void Main(string[] args)
        {
            if (args.Length != 1)
            {
                Console.WriteLine("Argument Error");
                return;
            }

            string INTPUT_FILE = args[0];

            try
            {
                // Read cardinalities if all characters from input file
                var cardinalities = ReadCharacterCardinalities(INTPUT_FILE);

                // Empty file
                if (cardinalities.Count == 0)
                    return;

                var treeCollection = new TreeCollection(cardinalities);

                // Read cardinalities, build sorted collection above cardinalities and compute huffman tree above it
                Tree huffmanTree = BuildHuffmanTree(treeCollection);

                // Print the result tree                
                huffmanTree.PrintTree(System.Console.Out);
            }
            catch (Exception)
            {
                Console.WriteLine("File Error");
            }
        }
        
        /// <summary>
        /// Takes the two "smalest" trees from collection, composes them to the bigger tree.
        /// 
        /// Does this until there is only the last tree - the Huffman tree.
        /// </summary>
        /// <param name="treeCollection">Ascending-sorted collection of trees</param>
        /// <returns>Returns the huffman tree</returns>
        private static Tree BuildHuffmanTree(TreeCollection treeCollection)
        {
            // Take 2 "smallest" trees, add one
            while (treeCollection.TreeCount >= 2)
            {
                // Remove the two trees by specified order and compose them to the tree that is added to trees collection
                treeCollection.Add(new Tree(treeCollection.SmallestTree, treeCollection.SmallestTree));
            }

            // Returns the remaining tree
            return treeCollection.SmallestTree;
        }
        
        /// <summary>
        /// Reads file and collects cardinalities for every character in the file
        /// </summary>
        /// <param name="filePath">Path to the file</param>
        /// <param name="cardinalities">Cartinalities of all characters in the file</param>
        private static Dictionary<int, long> ReadCharacterCardinalities(string filePath)
        {
            Dictionary<int, long> cardinalities = new Dictionary<int, long>();

            // Read cardinalities
            FileStream reader = new FileStream(filePath, FileMode.Open);
            try
            {
                int maxSize = 16000;
                int character, length;
                byte[] buffer = new byte[maxSize];

                while ((length = reader.Read(buffer, 0, maxSize)) > 0)
                {
                    int index = 0;
                    while (index < length)
                    {
                        // Read buffered byte
                        character = buffer[index];

                        // Add to dictionary
                        if (!cardinalities.ContainsKey(character))
                            cardinalities.Add(character, 1);
                        else
                            ++cardinalities[character];

                        ++index;
                    }
                }
            }
            finally
            {
                reader.Close();
            }

            return cardinalities;
        } 
    }

    /// <summary>
    /// Sorted collection of trees.
    /// 
    /// Implemented as two priority queues - where tree weight is a priority. One queue for leaf nodes, second for the trees.
    /// </summary>
    class TreeCollection
    {
        private List<Tree> leafQueue = new List<Tree>();
        private List<Tree> treeQueue = new List<Tree>();

        /// <summary>
        /// Return the first tree with the lowest weight. This tree is removed from the collection
        /// </summary>
        public Tree SmallestTree
        {
            get
            {
                // Just check
                if (leafQueue.Count == 0 && treeQueue.Count == 0)
                    return null;

                --TreeCount;

                // Both queues are not empty
                if (leafQueue.Count > 0 && treeQueue.Count > 0)
                {
                    if (leafQueue[0].Weight <= treeQueue[0].Weight)
                    {
                        var tree = leafQueue[0];

                        leafQueue.RemoveAt(0);

                        return tree;
                    }
                    else
                    {
                        var tree = treeQueue[0];

                        treeQueue.RemoveAt(0);

                        return tree;
                    }
                }
                // treeQueue is not empty
                else if (leafQueue.Count == 0)
                {
                    var tree = treeQueue[0];

                    treeQueue.RemoveAt(0);

                    return tree;
                }
                // leafQueue is not empty
                else
                {
                    var tree = leafQueue[0];

                    leafQueue.RemoveAt(0);

                    return tree;
                }
            }
        }

        /// <summary>
        /// Gets count of trees in the collection
        /// </summary>
        public int TreeCount { get; private set; }

        /// <summary>
        /// Builds a collection of leaf-noded trees based on (character, cardinality) pairs
        /// </summary>
        /// <param name="cardinalities">(character, cardinality) pairs collection</param>
        public TreeCollection(Dictionary<int, long> cardinalities)
        {
            // TODO: Sort this collecton effectively
            var collection = from p in cardinalities
	                        orderby p.Value, p.Key
	                        select p;

            // Fill the collection with leaf-nodes
            // The order by character ensures that characters with the same weight are ordered properly (the lowest first)
            foreach (var pair in collection)
            {
                leafQueue.Add(new Tree(new LeafNode(pair.Key, pair.Value)));
            }

            TreeCount = leafQueue.Count;
        }

        /// <summary>
        /// Adds tree to the collection
        /// </summary>
        /// <param name="tree">Tree to add</param>
        public void Add(Tree tree)
        {
            treeQueue.Add(tree);
            ++TreeCount;
        }        

        /// <summary>
        /// Weight comparer
        /// </summary>
        private class SortWeightAscending : IComparer<long>
        {
            public int Compare(long x, long y)
            {
                return x.CompareTo(y);
            }
        }
    }   

    /// <summary>
    /// Class that represents Huffman-coding tree.
    /// 
    /// Tree is defined as
    /// Tree = LeafNode | (LeftSubTree, RightSubTree) where Subtrees are Trees.
    /// 
    /// Invariants: 
    /// 
    ///     (LeafNode == null) || (Left == null && Right == null).
    ///     Left.Weight &lt;= Rigth.Weight
    /// </summary>
    class Tree
    {
        /// <summary>
        /// Gets LeafNode or null if tree has Left and Rigtht subrees
        /// </summary>
        public LeafNode Leaf { get; private set; }

        /// <summary>
        /// Gets the left subtree or null if Tree has LeafNode
        /// </summary>
        public Tree Left { get; private set; }

        /// <summary>
        /// Gets the right subtree or null if Tree has LeafNode
        /// </summary>
        public Tree Right { get; private set; }

        /// <summary>
        /// Gets weigth of the tree as weight of LeafNode od sum of both subtrees weights
        /// </summary>
        public long Weight { get; private set; }

        /// <summary>
        /// Constructor for creating tree with only a leaf node
        /// </summary>
        /// <param name="leaf">Leaf of current tree</param>
        public Tree(LeafNode leaf)
        {
            Leaf = leaf;
            Left = null;
            Right = null;

            Weight = Leaf.Weight;
        }

        /// <summary>
        /// Constructor for creating a tree with two subtrees and no leaf
        /// </summary>
        /// <param name="left">Left subtree</param>
        /// <param name="right">Right subtree</param>
        public Tree(Tree left, Tree right)
        {
            Leaf = null;

            Left = left;
            Right = right;

            Weight = Left.Weight + Right.Weight;
        }

        /// <summary>
        /// Prints the tree in specified format
        /// </summary>
        /// <param name="textWriter">Writer to write the output into</param>
        public void PrintTree(TextWriter textWriter)
        {
            if (Leaf == null)
            {
                // Print the tree in in-order notation
                textWriter.Write(Weight);
                textWriter.Write(" ");
                Left.PrintTree(textWriter);
                textWriter.Write(" ");
                Right.PrintTree(textWriter);
            }
            else
            {
                // Print leaf
                textWriter.Write(String.Format("*{0}:{1}", Leaf.Character, Leaf.Weight));
            }
        }
    }

    /// <summary>
    /// Class that represents Leaf of the tree.
    /// 
    /// Leafs contains Character value and its Weight (occurence count)
    /// </summary>
    class LeafNode
    {
        /// <summary>
        /// The corresponding character
        /// </summary>
        public int Character { get; private set; }

        /// <summary>
        /// The occurence count of the Character
        /// </summary>
        public long Weight { get; private set; }

        /// <summary>
        /// Defualt .ctor
        /// </summary>
        /// <param name="character">Byte representation of Character</param>
        /// <param name="weight">Its weight (occurence count)</param>
        public LeafNode(int character, long weight)
        {
            Character = character;
            Weight = weight;
        }
    }
}