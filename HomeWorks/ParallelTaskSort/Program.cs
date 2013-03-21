using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ParallelTaskSort
{
    class Program
    {
        private static readonly int BUFFER_SIZE = 10;
        private static readonly Queue<List<int>> sortedListsQueue = new Queue<List<int>();

        /*
         * Maximal paralelization of MERGE tasks
         * Using System.Threading.Tasks.*
         * Do not use Thread, ThreadPool
         * Task(<T>).Wait, WaitALL, WaitAny, ContinueWith <==> TaskFactory.ContinueWithAll/Any(b)
         * 
         * Read 10kB block of intput, then Array.Sort while reading another 10kB input block
         * When 2 blocks sorted, merge them, etc. 
         * While merging 2 last bits, and some part is already merged, can send to output while still merging the rest of those 2 bits
         */
        static void Main(string[] args)
        {
            List<int> buffer1 = new List<int>(10) {5, 9, 3, 6, 0, 7, 8};
            List<int> buffer2 = new List<int>(10) {15, 6, 4, 51, 1, 18, 91, -8};

            Task<List<int>> t1 = new Task<List<int>>(() => Sort(buffer1));
            Task<List<int>> t2 = new Task<List<int>>(() => Sort(buffer2));

            t1.Start();
            t2.Start();

            Task merge = new Task(() => { Task.WaitAll(t1, t2); AddSortedResult(Merge(t1.Result, t2.Result)); } );
            merge.Start();

            
        }

        static void Write(List<int> list)
        {
            foreach (var n in list)
                Console.WriteLine(n);
        }

        static void AddSortedResult(List<int> list)
        {
            sortedListsQueue.Enqueue(list);

            lock (sortedListsQueue)
            {
                if (sortedListsQueue.Count > 2)
                {
                    Task.Factory.StartNew(() => AddSortedResult(Merge(sortedListsQueue.Dequeue(), sortedListsQueue.Dequeue())));
                }
            }
        }

        static List<int> Sort(List<int> list)
        {
            list.Sort();

            return list;
        }

        static List<int> Merge(List<int> list1, List<int> list2)
        {
            if (list2 != null && list2.Count > 0)
            {
                list1.AddRange(list2);
                list1.Sort();
            }

            return list1;
        }
    }
}
