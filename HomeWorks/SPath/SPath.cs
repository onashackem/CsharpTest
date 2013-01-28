using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace SPath
{
    class SPath
    {
        static void Main(string[] args)
        {
            //a[*[2]]/c

            string input = String.Empty;
            using (StreamReader reader = new StreamReader("data.in"))
            {
                input = reader.ReadToEnd();
            }

            Tree tree = new TreeBuilder().BuildTree(new TreeParser(input));

            using(StreamReader reader = new StreamReader("query.in"))
            using(StreamWriter writer = new StreamWriter("results.out"))
            {
                string queryString = reader.ReadLine();

                if (String.IsNullOrEmpty(queryString) ||
                    String.IsNullOrEmpty((queryString = queryString.Replace(" ", "").Replace("\t", ""))))
                    return;

                TreeQuery query = new QueryBuilder().BuildTreeQuery(new QueryParser(queryString));

                var result = query.GetResults(tree.Root);

                foreach (var node in result)
                {
                    Console.WriteLine(String.Format("RESULT: {0}", node.ToString()));
                    if (node == tree.Root)
                        writer.WriteLine("/");
                    else
                        writer.WriteLine(String.Format("{0}", node.ToString()));
                }

                if (result.Count == 0)
                {
                    writer.WriteLine();
                }
            }
        }
    }

    class Tree
    {
        public TreeNode Root { get; set; }

        public Tree()
        {
            Root = new TreeNode(string.Empty, null);
        }
    }

    class TreeNode
    {
        public string Name  { get; private set; }

        public TreeNode Parent { get; private set; }

        public List<TreeNode> Children { get; private set; }

        private int _index = -1;
        public int Index 
        {
            get { return _index; }
            set 
            {
                if (_index != -1)
                    System.Diagnostics.Debug.Fail("Cannot set index twice!");
                
                _index = value;
            }
        }

        public TreeNode(string name, TreeNode parent)
        {
            Name = name;
            Parent = parent;
            Children = new List<TreeNode>(10);
        }

        public void AddChild(TreeNode child)
        {
            Children.Add(child);
            child.Index = Children.Count - 1;
        }

        public override string ToString()
        {
            // Root
            if (Parent == null)
                return String.Empty;

            return String.Format("{2}/{0}({1})", Name, Index, Parent.ToString());
        }
    }

    class TreeQuery
    {
        private List<IQuery> queries = new List<IQuery>(15);

        public void AddQuery(IQuery query)
        {
            queries.Add(query);
        }

        public List<TreeNode> GetResults(TreeNode root)
        {
            List<TreeNode> nodeQueue = new List<TreeNode>() { root };

            int queryIndex = 0;
            foreach (var query in queries)
            {
                List<TreeNode> currentResult = new List<TreeNode>(nodeQueue.Count * 5);

                query.Initialize();
                foreach (var node in nodeQueue)
                {
                    // Get nodes that satisfies query
                    foreach (var result in query.GetResults(node))
                    {
                        if (!currentResult.Contains(result))
                            currentResult.Add(result);
                    }
                }

                nodeQueue = currentResult;
            }

            return nodeQueue;
        }
    }

    abstract class BaseQuery : IQuery
    {
        protected List<IFilter> Filters { get; set; }

        protected BaseQuery()
        {
            Filters = new List<IFilter>(5);
        }

        public void AddFilter(IFilter filter)
        {
            Filters.Add(filter);
        }

        protected List<TreeNode> ApplyFilters(List<TreeNode> initialQueue)
        {
            foreach (var filter in Filters)
            {
                List<TreeNode> currentResult = new List<TreeNode>(initialQueue.Count);

                foreach (var node in initialQueue)
                {
                    if (filter.Matches(node))
                    {
                        currentResult.Add(node);
                    }
                }

                initialQueue = currentResult;

                if (currentResult.Count == 0)
                {
                    break;
                }
            }

            return initialQueue;
        }

        public abstract List<TreeNode> GetResults(TreeNode node);


        public void Initialize()
        {
            Filters.ForEach(f => f.Restore());
        }
    }

    class FilteredQuery : BaseQuery 
    {
        public override List<TreeNode> GetResults(TreeNode node)
        {
            return ApplyFilters(node.Children);
        }
    }

    class AllChildrenQuery : BaseQuery  
    {
        public override List<TreeNode> GetResults(TreeNode node)
        {
            return ApplyFilters(node.Children);
        }

    }

    class ParentQuery : BaseQuery
    {
        public override List<TreeNode> GetResults(TreeNode node)
        {
            return ApplyFilters(new List<TreeNode>() {node.Parent});
        }
    }

    class IdentifierFilter : IFilter
    {
        private string nodeIdentifer;

        public IdentifierFilter(string identifier)
        {
            this.nodeIdentifer = identifier;
        }
    
        public bool  Matches(TreeNode node)
        {
            return node.Name == nodeIdentifer;
        }

        public void Restore() { }

        public override string ToString()
        {
            return String.Format("ID: {0}", nodeIdentifer);
        }
    }

    class OrderFilter : IFilter
    {
        private readonly int nodeOrder;
        private int counter;

        public OrderFilter(int order)
        {
            this.nodeOrder = order;
            Restore();
        }

        public bool Matches(TreeNode node)
        {
            // Asserts that only the nodeOrder.th node will pass this filter
            return 0 == counter--;
        }

        public void Restore()
        {
            // Asserts that only the nodeOrder.th node will pass this filter
            counter = nodeOrder;
        }

        public override string ToString()
        {
            return String.Format("ORD: {1}/{0}", nodeOrder, nodeOrder - counter);
        }
    }

    class QueryFilter : IFilter
    {
        private TreeQuery subQuery;

        public QueryFilter(TreeQuery query)
        {
            this.subQuery = query;
        }

        public bool Matches(TreeNode node)
        {
            return subQuery.GetResults(node).Count > 0;
        }

        public void Restore() { }

        public override string ToString()
        {
            return String.Format("QF");
        }
    }

    interface IFilter
    {
        bool Matches(TreeNode node);

        void Restore();
    }

    interface IQuery
    {
        List<TreeNode> GetResults(TreeNode node);

        void AddFilter(IFilter filter);

        void Initialize();
    }

    class QueryBuilder
    {
        public TreeQuery BuildTreeQuery(QueryParser parser)
        {
            TreeQuery query = new TreeQuery();   
            while (parser.HasNextToken()) 
            {
                var q = BuildQuery(parser);

                if (q != null)
                    query.AddQuery(q);
            }

            return query;
        }

        private IQuery BuildQuery(QueryParser parser)
        {
            IQuery currentQuery = null;
            string token = parser.GetCurrentToken();

            // Skip initial "/" (subquery doesn't start with it)
            if (token == "/")
            {
                if (parser.HasNextToken())
                    token = parser.GetNextToken();
                else
                    return null;
            }

            // First read query type
            // * -- all chiildren
            // .. -- Parent
            // identifier -- Node identifier
            if (token == "*")
            {
                currentQuery = new AllChildrenQuery();

                if (parser.HasNextToken())
                    token = parser.GetNextToken();
            }
            else if (token == "." && parser.HasNextToken() && (token = parser.GetNextToken()) == ".")
            {
                currentQuery = new ParentQuery();

                if (parser.HasNextToken())
                    token = parser.GetNextToken();
            }
            else
            {
                currentQuery = new FilteredQuery();

                string identifier = token;

                while (parser.HasNextToken() && (token = parser.GetNextToken()) != "[" && token != "]" && token != "/")
                {
                    identifier += token;
                }

                currentQuery.AddFilter(new IdentifierFilter(identifier));
            }
    
            // Then add filters if defined
            // [Node order] or [SubQuery]
            while (token == "[")
            {
                token = parser.GetNextToken();
                int order;

                if(Int32.TryParse(token, out order))
                {
                    string number = token;
                    while((token = parser.GetNextToken()) != "]")
                    {
                        number += token;
                    }

                    if(Int32.TryParse(number, out order))
                    {
                        currentQuery.AddFilter(new OrderFilter(order));
                    }
                }
                else
                {
                    // Add subqueries to current query as another filter
                    TreeQuery treeQuery = new TreeQuery();
                    do
                    {
                        treeQuery.AddQuery(BuildQuery(parser));
                    }
                    while ((token = parser.GetCurrentToken()) == "/");

                    // Add parsed TreeQuery
                    currentQuery.AddFilter(new QueryFilter(treeQuery));
                }
                
                if (token == "]")
                    if (parser.HasNextToken())
                        token = parser.GetNextToken();
            }

            return currentQuery;
        }
    }

    class TreeBuilder
    {
        public Tree BuildTree(TreeParser parser)
        {
            var tree = new Tree();
                
            BuildTreeNodes(parser, tree.Root);

            return tree;
        }

        private void BuildTreeNodes(TreeParser parser, TreeNode parent)
        {
            TreeNode currentNode = null;
            while (parser.HasNextToken())
            {
                string token = parser.GetNextToken();
                System.Diagnostics.Debug.Assert(token != null, "Node token is null");

                switch (token)
                {
                    case "(":
                        System.Diagnostics.Debug.Assert(currentNode != null, "OOPS ... (");
                        BuildTreeNodes(parser, currentNode);
                        break;

                    case ")":
                        System.Diagnostics.Debug.Assert(currentNode != null, "OOPS ... )");
                        return;

                    default: // Identifier
                        currentNode = new TreeNode(token, parent);
                        parent.AddChild(currentNode);

                        break;
                }
            }
        }
    }

    interface IParser
    {
        bool HasNextToken();

        string GetNextToken();
    }

    class TreeParser : IParser
    {
        string[] tokens;
        int nextTokenIndex = 0;

        public TreeParser(string input) 
        {
            tokens = input.Split(new string[] {" ", "\t", "\n", "\r"}, StringSplitOptions.RemoveEmptyEntries);
        }

        public bool HasNextToken()
        {
            // No more tokens
            return nextTokenIndex < tokens.Length;
        }

        public string GetNextToken()
        {
            if (!HasNextToken())
                return null;

            // Return next token and increase the counter
            return tokens[nextTokenIndex++];
        }
    }

    class QueryParser : IParser
    {
        string tokens;
        int nextTokenIndex = 0;
        public QueryParser(string input)
        {
            this.tokens = input;
        }

        public bool HasNextToken()
        {
            return nextTokenIndex < tokens.Length;
        }

        public string GetNextToken()
        {
            return tokens[nextTokenIndex++].ToString();
        }

        public string GetCurrentToken()
        {
            if (nextTokenIndex == 0)
                return GetNextToken();

            return tokens[nextTokenIndex - 1].ToString();
        }

        public void StepBack()
        {
            nextTokenIndex = Math.Max(nextTokenIndex - 1, 0);
        }
    }
}
