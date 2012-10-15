using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TreeAlgorithms.Programs.Base;
using TreeAlgorithms.Concrete;
using TreeAlgorithms.Base;

namespace TreeAlgorithms.Programs
{
    class Bag : IProgram
    {
        private const int PRICE_LIMIT = 10;
        private const int WEIGHT_LIMIT = 15;

        public void run(string[] args)
        {
            // Bag of 
            List<BagItem> bag = new List<BagItem>()
            {
                new BagItem(0, 0, "0"),
                new BagItem(1, 2, "1"),                
                new BagItem(3, 3, "2"),
                new BagItem(2, 2, "3"),
                new BagItem(3, 4, "4"),
                new BagItem(2, 3, "5"),
                new BagItem(1, 3, "6"),
                new BagItem(2, 4, "7"),
                new BagItem(1, 1, "8"),
            };

            // Build tree for bag, remove the initial empty bag item
            BinaryTree<int, int> tree = BuildTree(bag);
            bag.RemoveAt(0);

            foreach (var optimum in FindOptimalLoad(tree, bag))
            {
                for (int i = 0; i < optimum.Length; ++i)
                {
                    if (optimum[i] == '1')
                        Console.Write(bag[i].Name + " ");
                }

                Console.WriteLine(); 
            }
        }

        private List<string> FindOptimalLoad(BinaryTree<int, int> tree, List<BagItem> bag)
        {
            // <weight, path+> (path is a way how to achieve certain weight)
            Dictionary<int, List<string>> fullBags = new Dictionary<int, List<string>>();

            // Generate fill dictionary with path how to fill the bag
            ProcessNode(tree.Root, "", fullBags, tree.Root.Key, tree.Root.Value);

            // Take the best weight
            int bestWeight = fullBags.Keys.Max();
            Console.WriteLine("Best weight: " + bestWeight);

            
            // Find the best price
            Dictionary<int, List<String>> bagPrices = new Dictionary<int,List<string>>();
            foreach (var path in fullBags[bestWeight])
            {
                var price = 0;
                for (int i = 0; i < path.Length; ++i)
                {
                    if (path[i] == '1')
                        price += bag[i].Price;
                }

                if (!bagPrices.ContainsKey(price))
                    bagPrices.Add(price, new List<string>());

                if (!bagPrices[price].Contains(path))
                    bagPrices[price].Add(path);
            }

            int bestPrice = bagPrices.Keys.Min();
            Console.WriteLine("Best price: " + bestPrice);

            return bagPrices[bestPrice];
        }

        private void ProcessNode(INodeBase<int, int> node, string path, Dictionary<int, List<string>> fullBags, int currentPrice, int currentWeight)
        {
            // Method is called only when item fits the bag -> remember it
            if (path != "")
            {                
                if (!fullBags.ContainsKey(currentWeight))
                    fullBags.Add(currentWeight, new List<string>());

                if (!fullBags[currentWeight].Contains(path))
                    fullBags[currentWeight].Add(path);
            }

            var nextItemNode = node.TopLeftChild;
            if (nextItemNode == null)
                return;

            // If next item fits the bag, add it to the bag
            if (currentPrice + nextItemNode.Key <= PRICE_LIMIT && currentWeight + nextItemNode.Value < WEIGHT_LIMIT)
            {
                ProcessNode(nextItemNode, path + "1", fullBags, currentPrice + nextItemNode.Key, currentWeight + nextItemNode.Value);
            }

            // Also do not add it to the bag in every case
            ProcessNode(node.TopRightChild, path + "0", fullBags, currentPrice, currentWeight);
        }

        private BinaryTree<int, int> BuildTree(List<BagItem> bag)
        {
            BinaryNode<int, int> root = new BinaryNode<int,int>(bag[0].Price, bag[0].Weight);
            BinaryTree<int, int> tree = new BinaryTree<int, int>(root);

            List<BinaryNode<int, int>> currentLayer = new List<BinaryNode<int, int>>() { root };

            for (var i = 1; i < bag.Count; ++i)
            {
                List<BinaryNode<int, int>> nodeQueue = new List<BinaryNode<int, int>>() { root };
                foreach (var node in currentLayer)
                {
                    // Create left and right son
                    var leftNode = new BinaryNode<int, int>(bag[i].Price, bag[i].Weight);
                    var rightNode = new BinaryNode<int, int>(bag[i].Price, bag[i].Weight);

                    // Append to current node
                    node.AddChild(leftNode, 0);
                    node.AddChild(rightNode, 1);

                    // add to queue
                    nodeQueue.Add(leftNode);
                    nodeQueue.Add(rightNode);
                }

                // Go to the new layer
                currentLayer = nodeQueue;
            }

            return tree;
        }
    }
}
