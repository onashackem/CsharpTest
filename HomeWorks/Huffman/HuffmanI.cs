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
                // Read cardinalities if all characters from input file, fill the collection from that
                var treeCollection = new TreeCollection();
                treeCollection.FillCollectionFromFile(INTPUT_FILE);

                // Compute huffman tree above it
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
        /// <param name="sortedTrees">Ascending-sorted collection of trees</param>
        /// <returns>Returns the huffman tree</returns>
        private static Tree BuildHuffmanTree(TreeCollection sortedTrees)
        {
            // Take 2 "smallest" trees, add one
            while (sortedTrees.TreesCount > 1)
            {
                // Remove the two trees by specified order and compose them to the tree that is added to trees collection
                sortedTrees.Add(new Tree(sortedTrees.SmallestTree, sortedTrees.SmallestTree));
            }

            // Returns the remaining tree
            return sortedTrees.SmallestTree;
        }

        

        /// <summary>
        /// Sorted collection of trees
        /// </summary>
        class TreeCollection
        {
            /// <summary>
            /// The collection is keyed by weight of trees. The value is list of the tree with the proper weight.
            /// The list works as a queue - new trees belongs to the end, the trees are taken from the beginning.
            /// </summary>
            private SortedDictionary<long, LinkedList<Tree>> trees;

            /// <summary>
            /// Return the first tree with the lowest weight. This tree is removed from the collection
            /// </summary>
            public Tree SmallestTree
            {
                get
                {
                    // Gets the List of trees with the lowest weight
                    var lowestPair = trees.First();

                    // Clear from the empty
                    if (lowestPair.Value.Count == 0)
                    {
                        trees.Remove(lowestPair.Key);
                        lowestPair = trees.First();
                    }

                    // Get the first tree from tree list - trees are already in proper order
                    var tree = lowestPair.Value.First.Value;

                    // Remove the tree from collection
                    lowestPair.Value.RemoveFirst();
                    --TreesCount;

                    // Return the first tree with the lowes weight
                    return tree;
                }
            }

            /// <summary>
            /// Gets count of trees in the collection
            /// </summary>
            public int TreesCount { get; private set; }

            /// <summary>
            /// Default 
            /// </summary>
            public TreeCollection()
            {
                trees = new SortedDictionary<long, LinkedList<Tree>>();
                TreesCount = 0;
            }

            /// <summary>
            /// Adds tree to the collection
            /// </summary>
            /// <param name="tree">Tree to add</param>
            public void Add(Tree tree)
            {
                if (!trees.ContainsKey(tree.Weight))
                    trees.Add(tree.Weight, new LinkedList<Tree>());

                // Added trees always belongs to the end of the list of trees with the same weight
                // When collection is created, trees are composed from ordered leaf nodes
                // When adding composed trees, the new trees belongs to the end also
                trees[tree.Weight].AddLast(tree);
                ++TreesCount;
            }
            
            /// <summary>
            /// Reads file and collects cardinalities for every character in the file.
            /// Then builds collection of leaf-noded from that data.
            /// </summary>
            /// <param name="filePath">Path to the file</param>
            public void FillCollectionFromFile(string filePath)
            {
                Dictionary<int, Tree> cardinalities = new Dictionary<int, Tree>();

                // Read cardinalities in leaf-noded trees
                using (FileStream reader = new FileStream(filePath, FileMode.Open))
                {
                    int character;
                    while ((character = reader.ReadByte()) > -1)
                    {
                        if (!cardinalities.ContainsKey(character))
                            cardinalities.Add(character, new Tree(new LeafNode(character, 1)));
                        else
                            cardinalities[character].Leaf.IncreaseWeight();
                    }
                }

                // Fill the collection with those trees
                foreach (var pair in cardinalities.OrderBy(p => p.Key))
                {
                    var weight = pair.Value.Weight;

                    if (!trees.ContainsKey(weight))
                        trees.Add(pair.Value.Weight, new LinkedList<Tree>());
                    
                    trees[weight].AddLast(pair.Value);
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
            /// Constructor ofr creating a tree with two subtrees and no leaf
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

            /// <summary>
            /// Only a debug-check function that checks the proper order of 2 trees
            /// </summary>
            /// <param name="otherTree">Tree to compare this tree to</param>
            /// <returns>Returns true id this tree belongs before the other tree</returns>
            public bool IsLighterThan(Tree otherTree)
            {
                // Compare weights
                if (Weight != otherTree.Weight)
                    return Weight < otherTree.Weight;

                // Compare characters if both trees have only leafs
                if (this.Leaf != null && otherTree.Leaf != null)
                    return this.Leaf.Character < otherTree.Leaf.Character;

                // If this node has no leaf, the other node shouldn't have a leaf either
                // The proper order of leafless trees is ensured by SortTreesAscending comparer.
                return !(this.Leaf == null && otherTree.Leaf != null);
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
            /// Increases node weight by 1
            /// </summary>
            public void IncreaseWeight()
            {
                ++Weight;
            }
        }
    }
}