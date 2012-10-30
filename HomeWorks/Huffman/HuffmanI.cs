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
                new Compressor(INTPUT_FILE).Compress();
            }
            catch (Exception)
            {
                Console.WriteLine("File Error");
            }
        }
        
    }

    /// <summary>
    /// Sorted collection of trees.
    /// 
    /// Implemented as two priority queues - where tree weight is a priority. One queue for leaf nodes, second for the trees.
    /// </summary>
    class TreeBuilder
    {
        private readonly LinkedList<Tree> leafQueue = new LinkedList<Tree>();
        private readonly LinkedList<Tree> treeQueue = new LinkedList<Tree>();

        private readonly Dictionary<int, long> cardinalities = new Dictionary<int, long>();
        private readonly string inputFilePath;

        /// <summary>
        /// Return the first tree with the lowest weight. This tree is removed from the collection
        /// </summary>
        private Tree SmallestTree
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
                    if (leafQueue.First.Value.Weight <= treeQueue.First.Value.Weight)
                    {
                        var tree = leafQueue.First.Value;

                        leafQueue.RemoveFirst();

                        return tree;
                    }
                    else
                    {
                        var tree = treeQueue.First.Value;

                        treeQueue.RemoveFirst();

                        return tree;
                    }
                }
                // treeQueue is not empty
                else if (leafQueue.Count == 0)
                {
                    var tree = treeQueue.First.Value;

                    treeQueue.RemoveFirst();

                    return tree;
                }
                // leafQueue is not empty
                else
                {
                    var tree = leafQueue.First.Value;

                    leafQueue.RemoveFirst();

                    return tree;
                }
            }
        }

        /// <summary>
        /// Gets count of trees in the collection
        /// </summary>
        private int TreeCount { get; set; }

        /// <summary>
        /// Default .ctor
        /// </summary>
        /// <param name="filePath">Path to read build the tree from</param>
        public TreeBuilder(string filePath)
        {
            this.inputFilePath = filePath;
        }

        /// <summary>
        /// Builds huffman tree from file specified in constructor.
        /// </summary>
        /// <param name="treeCollection">Ascending-sorted collection of trees</param>
        /// <returns>Returns the huffman tree</returns>        
        public Tree BuildHuffmanTree()
        {
            // Read cardinalities from input file
            ReadCardinalities();

            // Fill queue with leaf-noded trees
            FillLeafQueue();

            // Take 2 "smallest" trees, add one
            while (TreeCount >= 2)
            {
                // Remove the two trees by specified order and compose them to the tree that is added to trees collection
                Add(new Tree(SmallestTree, SmallestTree));
            }

            // Returns the remaining tree
            return SmallestTree;
        }

        /// <summary>
        /// Creates leaf-node for each character and its cardinality. The queue is orderd by weight and character ascending
        /// </summary>
        private void FillLeafQueue()
        {
            // TODO: Sort this collecton effectively
            var collection = from p in cardinalities
	                        orderby p.Value, p.Key
	                        select p;

            // Fill the collection with leaf-nodes
            // The order by character ensures that characters with the same weight are ordered properly (the lowest first)
            foreach (var pair in collection)
            {
                leafQueue.AddLast(new Tree(new LeafNode(pair.Key, pair.Value)));
            }

            TreeCount = leafQueue.Count;
        }

        /// <summary>
        /// Adds tree to the collection
        /// </summary>
        /// <param name="tree">Tree to add</param>
        private void Add(Tree tree)
        {
            treeQueue.AddLast(tree);
            ++TreeCount;
        } 
        
        /// <summary>
        /// Reads file and collects cardinalities for every character in the file
        /// </summary>
        private void ReadCardinalities()
        {
            // Read cardinalities
            using(var reader = new FileStream(this.inputFilePath, FileMode.Open))
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
        }     
    }

    public class Compressor
    {
        private List<byte> HEADER = new List<byte>() { 0x7B, 0x68, 0x75, 0x7C, 0x6D, 0x7D, 0x66, 0x66 };

        private readonly string inputFilePath;
        private readonly string outputFilePath;

        /// <summary>
        /// Inits input and output file paths
        /// </summary>
        /// <param name="inputFile"></param>
        public Compressor(string inputFile)
        {
            this.inputFilePath = inputFile;
            this.outputFilePath = String.Format("{0}.huff", inputFile);
        }

        /// <summary>
        /// Compresses file specified in constructor
        /// </summary>
        public void Compress()
        {
            var huffmanTree = new TreeBuilder(this.inputFilePath).BuildHuffmanTree();

            using (var writer = new FileStream(this.outputFilePath, FileMode.OpenOrCreate))
            using (var reader = new FileStream(this.inputFilePath, FileMode.Open))
            {
                WriteHeader(writer);

                WriteHuffmanTree(writer, huffmanTree);

                CompressFile(writer, reader);
            }
        }

        private void CompressFile(FileStream writer, FileStream reader)
        {
            throw new NotImplementedException();
        }

        private void WriteHuffmanTree(FileStream writer, Tree huffmanTree)
        {
            WriteTreeNode(writer, huffmanTree);
        }

        private void WriteTreeNode(FileStream writer, Tree node)
        {
            var bytes = CreateByteRepresentationForNode(node);

            if (node.Leaf == null)
            {
                WriteTreeNode(writer, node.Left);
                WriteTreeNode(writer, node.Left);
            }
        }

        private byte[] CreateByteRepresentationForNode(Tree node)
        {
            //TODO: BitConverter.IsLittleEndian

            var array = new bool[64];  

            array[0] = node.Leaf != null;

            var weight = node.Weight;
            for(int i = 1; i < 56; ++i)
            {
                if (weight >= 0)
                {
                    array[i] = Convert.ToBoolean(weight % 2);
                    weight >>= 1;
                }
                else
                {
                    array[i] = false;
                }
            }

            
            if (node.Leaf != null)
            {
                var character = node.Leaf.Character;
                for (int i = 56; i < 64; ++i)
                {
                    if (character >= 0)
                    {
                        array[i] = Convert.ToBoolean(character % 2);
                        character >>= 1;
                    }
                    else
                    {
                        array[i] = false;
                    }
                }
            }
            else
            {
                for (int i = 56; i < 64; ++i)
                {
                    array[i] = false;
                }
            }

            var bytes = new byte[8];
            return bytes;
        }

        private void WriteHeader(FileStream writer)
        {
            foreach (var b in HEADER)
            {
                writer.WriteByte(b);
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
    ///     (LeafNode == null) || (Left == null && Right == null).
    ///     Left.Weight &lt;= Rigth.Weight
    /// </summary>
    class Tree
    {
        /// <summary>
        /// Gets LeafNode or null if tree has Left and Rigtht sub
    /// rees
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