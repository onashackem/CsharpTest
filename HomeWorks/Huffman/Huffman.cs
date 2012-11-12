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
            string INPUT_FILE, OUTPUT_FILE;

            if (!ProcessArguments(args, out INPUT_FILE, out OUTPUT_FILE))
            {
                Console.WriteLine("Argument Error");
                return;
            }

            try
            {
                // Decompress the input file
                new Decompressor().Decompress(INPUT_FILE, OUTPUT_FILE);
            }
            catch (Exception e)
            {
                // Not an argument error -> file error
                Console.WriteLine("File Error");
            }
        }

        /// <summary>
        /// Extracts input and output file from arguments.
        /// </summary>
        /// <param name="args">Arguments collection</param>
        /// <param name="inputFile">Exctracted input file</param>
        /// <param name="outputFile">Extracted output file</param>
        /// <returns>Returns false if something doesn't fit, true otherwise</returns>
        private static bool ProcessArguments(string[] args, out string inputFile, out string outputFile)
        {
            // Intial values
            inputFile = outputFile = string.Empty;

            // Check arguments length
            if (args.Length != 1)
            {
                return false;
            }

            inputFile = args[0].ToLower();

            // Check input file has proper extension
            if (inputFile == null || !inputFile.EndsWith(".huff"))
            {
                return false;
            }

            outputFile = inputFile.Remove(inputFile.Length - 5);

            // Input file was only an extension
            if (outputFile.Length == 0)
            {
                return false;
            }

            // Input parameter is OK
            return true;
        }
        
    }

    /// <summary>
    /// This class decompresses compressed file. Parses Huffman tree from the file and decompresses the file content.
    /// </summary>
    public class Decompressor
    {
        /// <summary>
        /// Byte array to read bytes from the file
        /// </summary>
        private byte[] byteBuffer = new byte[8];

        private readonly byte[] HEADER = new byte[8] { 0x7B, 0x68, 0x75, 0x7C, 0x6D, 0x7D, 0x66, 0x66 };
        private readonly byte[] DELIMITTER = new byte[8] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x0, 0x00, 0x00 };

        /// <summary>
        /// The main method to decompress the specified file int specified output file.
        /// </summary>
        /// <param name="inputFile">The compressed input file</param>
        /// <param name="outputFile">The path to decompress the input file into</param>
        public void Decompress(string inputFile, string outputFile)
        {
            using (var writer = new FileStream(outputFile, FileMode.OpenOrCreate))
            using (var reader = new FileStream(inputFile, FileMode.Open))
            {
                // Read and validate file by header
                ReadHeader(reader);

                // Restore the coded Huffman tree
                Tree huffmanTree = RestoreHuffmanTree(reader);

                // Validate that TreeDelimiter is present
                ReadTreeDelimitter(reader);

                // Decode characters
                DecodeCharacters(reader, writer, huffmanTree);
            }
        }

        /// <summary>
        /// Restores the coded Huffman tree. When an error occured, and Exception is thrown.
        /// </summary>
        /// <param name="reader">Reader to input file. The position must be on the first tree node.</param>
        /// <returns>Returns restored huffman tree</returns>
        private Tree RestoreHuffmanTree(FileStream reader)
        {
            // Begin recursive call
            return RestoreNode(reader);
        }

        /// <summary>
        /// Recursive method to resotre tree coded by Compressor class in the infix notation.
        /// </summary>
        /// <param name="reader">Reader to coded file.</param>
        /// <returns>Returns restored node</returns>
        private Tree RestoreNode(FileStream reader)
        {
            // Read node from input
            int bytesRead = ReadBytes(reader);

            // Validate that end of the tree/file is not reached
            if (bytesRead != 8 || CompareBytes(byteBuffer, DELIMITTER))
                throw new Exception("Tree definition corrupted");

            // Convert bytes array to bit array
            bool[] nodeCode = CreateBitArrayBytes(byteBuffer);

            // Count weight
            long weight = CountWeight(nodeCode);

            // Count character
            int character = CountCharacter(nodeCode);

            // Leaf starts with 0 and has no children
            if (nodeCode[0])
            {
                return new Tree(new LeafNode(character, weight));
            }

            // Create both sub-trees
            Tree left = RestoreNode(reader);
            Tree right = RestoreNode(reader);
                     
            // Assert weight is properly coded
            if (weight != left.Weight + right.Weight)
                throw new Exception("Weight coded in the inner tree should equal to sum of subtrees weights");

            // Inner trees have no character
            if (character != 0)
                throw new Exception("Inner node should not code any tree");

            // Create and return an inner node
            return new Tree(left, right);
        }

        /// <summary>
        /// Decodes character from node code
        /// </summary>
        /// <param name="nodeCode">Coded node</param>
        /// <returns>Returns character</returns>
        private int CountCharacter(bool[] nodeCode)
        {
            return (int)ConvertFromBiary(nodeCode, 56, 64);
        }

        /// <summary>
        /// Decodes weight from node code
        /// </summary>
        /// <param name="nodeCode">Coded node</param>
        /// <returns>Returns weight</returns>
        private long CountWeight(bool[] nodeCode)
        {
            return ConvertFromBiary(nodeCode, 1, 56);
        }

        /// <summary>
        /// Converts specified binary field part to decimap
        /// </summary>
        /// <param name="binary">Binary code</param>
        /// <param name="startIndex">Start at this index</param>
        /// <param name="endIndex">End before this index</param>
        /// <returns>Returns converter part of binary field</returns>
        private long ConvertFromBiary(bool[] binary, int startIndex, int endIndex)
        {
            long result = 0;
            long powerOf2 = 1;
            for (int i = startIndex; i < endIndex; ++i)
            {
                if (binary[i])
                    result += powerOf2;

                powerOf2 <<= 1;
            }

            return result;
        }

        /// <summary>
        /// Converts given byte array to bit array - "Little Endian-ally" :-D
        /// </summary>
        /// <param name="bytes">Bytes to cenvert</param>
        /// <param name="bytesCount">Optionaly specify the count of bytes to convert</param>
        /// <returns>Returns bit array representation of the bytes</returns>
        private bool[] CreateBitArrayBytes(byte[] bytes, int bytesCount = -1)
        {
            if (bytesCount == -1)
                bytesCount = bytes.Length;

            bool[] bits = new bool[64];
                        
            // Every byte is coded into 8 bits
            for (int byteIndex = 0; byteIndex < bytesCount; ++byteIndex)
            {
                byte b = bytes[byteIndex];
                for (int bitIndex = 0; bitIndex < 8; ++bitIndex)
                {
                    bits[byteIndex * 8 + bitIndex] = (b % 2 == 1 ? true : false);
                    b >>= 1;
                }
            }

            return bits;
        }

        /// <summary>
        /// Reads the part of the input file that contains the coded characters and decodes them.
        /// </summary>
        /// <param name="reader">Reader from input file set to the first coded character</param>
        /// <param name="writer">Writer to output file</param>
        /// <param name="huffmanTree">Huffman tree that was used to code the file</param>
        private void DecodeCharacters(FileStream reader, FileStream writer, Tree huffmanTree)
        {
            // Iteration note through the tree
            Tree currentNode = huffmanTree;

            // Weight of the whole tree represents the number of all encoded characters 
            // (to skip the 0s at the end of the file)
            long charactersToPrint = huffmanTree.Weight;

            // Read until the end of the file -> read bytes into the byteBuffer
            int bytesRead = 0;
            while ((bytesRead = ReadBytes(reader)) > 0)
            {
                // Convert bytes to bit "stream" with encoded characters
                bool[] paths = CreateBitArrayBytes(byteBuffer, bytesRead);

                // Every true/false represents a move in the tree. 
                // When the iterator node reaches the Leaf node -> the character is encoded
                foreach(var direction in paths)
                {
                    // Left (false) or right (true)?
                    if (direction)
                        currentNode = currentNode.Right;
                    else
                        currentNode = currentNode.Left;

                    // End of the path is when the leaf if reached. Leaf contains character
                    if (currentNode.Leaf != null)
                    {
                        // Write decoded character
                        writer.WriteByte((byte)currentNode.Leaf.Character);

                        // Start from the tree root
                        currentNode = huffmanTree;

                        // Check all characters are printed -> the rest of the file is insignificant
                        if (--charactersToPrint == 0)
                            return;
                    }
                }
            }
        }

        /// <summary>
        /// Read and assert that Tree Delimiter is read properly. Throws Exception when not.
        /// </summary>
        /// <param name="reader">Reader from input file set before the expected tree delimiter</param>
        private void ReadTreeDelimitter(FileStream reader)
        {
            ReadBytes(reader);

            if (byteBuffer == DELIMITTER)
                throw new Exception("Delimitter is corrupted"); 
        }

        /// <summary>
        /// Read and assert that file header is read properly. Throws Exception when not.
        /// </summary>
        /// <param name="reader">Reader from input file set before the expected file header</param>
        private void ReadHeader(FileStream reader)
        {
            ReadBytes(reader);

            if (byteBuffer == HEADER)
                throw new Exception("Header is corrupted");            
        }

        /// <summary>
        /// Compares two bytes array whether contain the same bytes
        /// </summary>
        /// <param name="origin">First byte array</param>
        /// <param name="compareTo">Second byte array</param>
        /// <returns>Returns true if fields contain the same bytes</returns>
        private static bool CompareBytes(byte[] origin, byte[] compareTo)
        {
            return origin.SequenceEqual(compareTo);

            /*
            // Different lenght
            if (origin.Length != compareTo.Length)
                return false;

            // Different content
            for (int i = 0; i < origin.Length; ++i)
            {
                if (origin[i] != compareTo[i])
                    return false;
            }

            // Are the same
            return true;
             */
        }

        /// <summary>
        /// Reads bytes from input file into byteBuffer class member.
        /// </summary>
        /// <param name="reader">Reader to input file</param>
        /// <returns>Returns the number of read bytes</returns>
        private int ReadBytes(FileStream reader)
        {
            // Read byteBuffer
            for (int i = 0; i < byteBuffer.Length; ++i)
            {
                var b = reader.ReadByte();

                if (b == -1)
                    return i;

                byteBuffer[i] = (byte)b;
            }

            return byteBuffer.Length;
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