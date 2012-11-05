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
                // Compress the input file
                new Compressor().Compress(INTPUT_FILE);
            }
            catch (Exception e)
            {
                // Not an argument error -> file error
                Console.WriteLine("File Error");
            }
        }
        
    }

    /// <summary>
    /// This class is used to compress files
    /// </summary>
    public class Compressor
    {
        /// <summary>
        /// The bit representation of all characters
        /// </summary>
        bool[][] characterCodes;

        /// <summary>
        /// Compresses the specified file. Output file path is inputfilepath + ".huff" extension.
        /// </summary>
        /// <param name="inputFile">Path to file to compress</param>
        public void Compress(string inputFile)
        {
            string outputFile = String.Format("{0}.huff", inputFile);

            var huffmanTree = new TreeBuilder().BuildHuffmanTree(inputFile);

            using (var writer = new FileStream(outputFile, FileMode.OpenOrCreate))
            using (var reader = new FileStream(inputFile, FileMode.Open))
            {
                // File header
                WriteBytes(writer, new byte[8] { 0x7B, 0x68, 0x75, 0x7C, 0x6D, 0x7D, 0x66, 0x66 });

                // Huffman tree
                WriteHuffmanTreeAndGenerateCharacterCodes(writer, huffmanTree);

                // Delimitter
                WriteBytes(writer, new byte[8] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x0, 0x00, 0x00 });

                // Compressed file
                CompressFile(writer, reader);
            }
        }

        /// <summary>
        /// Reads the input file again and compresses it
        /// </summary>
        /// <param name="writer">Writer to write the compressed file content</param>
        /// <param name="reader">Reader from input file</param>
        private void CompressFile(FileStream writer, FileStream reader)
        {
            short inputBufferSize = 16000, bitArraySize = 256;
            byte[] inputBuffer = new byte[inputBufferSize];
            bool[] outputBits = new bool[bitArraySize];
            short bitArrayIndex = 0;

            // Read the file and compress every character
            int character, length;
            while ((length = reader.Read(inputBuffer, 0, inputBufferSize)) > 0)
            {
                short index = 0;
                while (index < length)
                {
                    // Read buffered byte
                    character = inputBuffer[index];

                    // Add coded characetr to the output byte
                    foreach (var bit in characterCodes[character])
                    {
                        // Increase index after value is returned
                        outputBits[bitArrayIndex++] = bit;

                        // Bit buffer is full -> write to output
                        if (bitArrayIndex == bitArraySize)
                        {
                            // Converts bits to bytes and write the whole buffer
                            WriteBytes(writer, CreateByteArrayFromBitRepresentation(outputBits));
                            bitArrayIndex = 0;
                        }
                    }

                    ++index;
                }
            }

            // Write the rest of the output buffer
            if (bitArrayIndex > 0)
            {
                // Add fill zeroes to complete the byte
                while (bitArrayIndex % 8 > 0)
                    outputBits[bitArrayIndex++] = false;

                // Converts bits to bytes and write the only the filled buffer part
                WriteBytes(writer, CreateByteArrayFromBitRepresentation(outputBits, bitArrayIndex));
            }
        }

        /// <summary>
        /// Writes byte representation of whole tree to output file in infix notation.
        /// Counts bit-path to the characters in the process.
        /// </summary>
        /// <param name="writer">Writer to write the tree into</param>
        /// <param name="node">Current node to write</param>
        /// <param name="currentPath">Current bit-path to the node</param>
        private void WriteHuffmanTreeAndGenerateCharacterCodes(FileStream writer, Tree huffmanTree)
        {
            // Init the character bit codes
            characterCodes = new bool[256][];

            // Writes the whole tree into the output file. The bit-path to the root is "". 
            WriteTreeNodeAndCollectCode(writer, huffmanTree, string.Empty);
        }

        /// <summary>
        /// Writes byte representation of node (inner tree or leaf) to output file in infix notation.
        /// Counts bit-path to the leafs (contain characters) in the process.
        /// </summary>
        /// <param name="writer">Writer to write the tree into</param>
        /// <param name="node">Current node to write</param>
        /// <param name="currentPath">Current bit-path to the node</param>
        private void WriteTreeNodeAndCollectCode(FileStream writer, Tree node, string currentPath)
        {
            // Write weight
            var bytes = CreateByteRepresentationForNode(node);
            WriteBytes(writer, bytes);

            // Add code for character
            if (node.Leaf != null)
            {
                // Build array from string contating bit-path to the character
                var path = new bool[currentPath.Length];
                for(int index = 0; index < currentPath.Length; ++index)
                {
                    path[index] = currentPath[index] == '1';
                }
                
                characterCodes[node.Leaf.Character] = path;

                return;
            }
            
            // Write Left node. Since is left, all sub-nodes are coded by '0'
            WriteTreeNodeAndCollectCode(writer, node.Left, currentPath + "0");

            // Write right node. Since is left is sub-nodes are coded by '1'
            WriteTreeNodeAndCollectCode(writer, node.Right, currentPath + "1");
        }

        /// <summary>
        /// Computes bytes that represents tree node.
        /// </summary>
        /// <param name="node">Node to compress</param>
        /// <returns>Returns byte conversion of the node</returns>
        private byte[] CreateByteRepresentationForNode(Tree node)
        {
            var bitRepresentation = new bool[64];

            // Frst bit (0 = inner node, 1 = leaf)
            bitRepresentation[0] = (node.Leaf != null);

            // 55 bits for weight
            var weight = node.Weight;
            for(int i = 1; i < 56; ++i)
            {
                bitRepresentation[i] = (weight % 2 == 1);
                weight >>= 1;
            }

            // 8 bits for coding character (if any)
            var character = (node.Leaf != null) ? node.Leaf.Character : 0;
            for (int i = 56; i < 64; ++i)
            {
                bitRepresentation[i] = (character % 2 == 1);
                character >>= 1;
            }

            // The bit  array is converted to 8bytes
            return CreateByteArrayFromBitRepresentation(bitRepresentation);
        }

        /// <summary>
        /// Creates bytes represented by bits. Using Little Endian format to convert.
        /// 
        /// 1101 0010 0001 1010 1110 0000 -> 0x4B 0x58 0x07
        /// </summary>
        /// <param name="bitRepresentation">Bit array (size id dividable by 8)</param>
        /// <param name="representationSize">The maximum count of bits to convert to byte. If not specified, all bits are converted.</param>
        /// <returns>Return bytes</returns>
        private byte[] CreateByteArrayFromBitRepresentation(bool[] bitRepresentation, int representationSize = -1)
        {
            if (representationSize == -1)
                representationSize = bitRepresentation.Length;

            byte[] byteRepresentation = new byte[representationSize / 8];

            for (int i = 0; i < byteRepresentation.Length; ++i)
            {
                // Start from the 8th bit and build the byte from it descending (using the bit-shift)
                int index = i * 8 + 7;
                byte b = (byte)(bitRepresentation[index] ? 1 : 0);

                // Process 7 more bits
                for (int j = 0; j < 7; ++j)
                {
                    b <<= 1;
                    b += (byte)(bitRepresentation[--index] ? 1 : 0);
                }

                byteRepresentation[i] = b;
            }

            return byteRepresentation;
        }

        /// <summary>
        /// Writes bytes to the writer
        /// </summary>
        /// <param name="writer">Writer to writy bytes to</param>
        /// <param name="bytes">Bytes to write to writer</param>
        private void WriteBytes(FileStream writer, byte[] bytes)
        {
            foreach (var b in bytes)
            {
                writer.WriteByte(b);
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
        /// <summary>
        /// Queue of created leafs - contains characters and their weights
        /// </summary>
        private LinkedList<Tree> leafQueue;

        /// <summary>
        /// Queue of inner trees - trees that are created during computation
        /// </summary>
        private LinkedList<Tree> treeQueue;

        /// <summary>
        /// Weights of characters
        /// </summary>
        private LeafNode[] cardinalities;

        /// <summary>
        /// Return the first tree with the lowest weight. This tree is removed from the queue.
        /// </summary>
        private Tree SmallestTree
        {
            get
            {
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
                // TreeQueue is not empty
                else if (leafQueue.Count == 0)
                {
                    var tree = treeQueue.First.Value;

                    treeQueue.RemoveFirst();

                    return tree;
                }
                // LeafQueue is not empty
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
        /// Builds huffman tree from file specified in constructor.
        /// </summary>
        /// <param name="treeCollection">Ascending-sorted collection of trees</param>
        /// <param name="filePath">Path to read build the tree from</param>
        /// <returns>Returns the huffman tree</returns>        
        public Tree BuildHuffmanTree(string filePath)
        {
            // Init cardinalities for every character (0-255)
            cardinalities = new LeafNode[256];

            // Read cardinalities from input file
            ReadCardinalities(filePath);

            // Init both queues
            leafQueue = new LinkedList<Tree>();
            treeQueue = new LinkedList<Tree>();

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
            // Fill the collection with leaf-nodes
            // The order by character ensures that characters with the same weight are ordered properly (the lowest first)
            foreach(var leaf in cardinalities.Where(l => l != null).OrderBy(l => l.Weight))
            {
                leafQueue.AddLast(new Tree(leaf));
            }
            
            TreeCount = leafQueue.Count;

            // Free memory
            cardinalities = null;
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
        /// Reads specified file and collects cardinalities for every character in the file
        /// </summary>
        /// <param name="inputFilePath">Input file to read</param>
        private void ReadCardinalities(string inputFilePath)
        {
            // Read cardinalities
            using (var reader = new FileStream(inputFilePath, FileMode.Open))
            {
                int maxSize = 16000;
                int length;
                byte[] buffer = new byte[maxSize];

                while ((length = reader.Read(buffer, 0, maxSize)) > 0)
                {
                    int index = 0;
                    while (index < length)
                    {
                        // Read buffered byte
                        int character = buffer[index];
                                                
                        if (cardinalities[character] == null)
                            cardinalities[character] = new LeafNode(character, 1);
                        else
                            // Increase weight
                            cardinalities[character].IncreaseWeight();

                        ++index;
                    }
                }
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
        /// Gets LeafNode or null if tree has Left and Rigtht sub trees
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

        /// <summary>
        /// Increases weight of this Leaf by 1
        /// </summary>
        public void IncreaseWeight()
        {
            ++Weight;
        }
    }
}