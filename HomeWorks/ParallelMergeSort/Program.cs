using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace ParallelMergeSort
{
    class Program
    {
        static void Main(string[] args)
        {
            // Get number of allowed threads
            var threadCount = GetThreadCount(args);
            if (threadCount < 1)
                return;

            // Read numbers to sort
            var inputs = GetInputs();
            if (inputs == null)
                return;

            // No sense to parallelize
            if (inputs.Count < 2 * threadCount)
                threadCount = 1;

            var task = new MergeTaks(inputs);
            var pool = new ThreadPool(threadCount - 1);

            pool.ProcessTask(task);

            task.PrintResult();

            /* TEST
            long time = 0;
            for (int i = 0; i < 100; ++i)
            {
                inputs = GetInputs();
                task = new MergeTaks(inputs);
                pool = new ThreadPool(2);

                System.Diagnostics.Stopwatch watch = new System.Diagnostics.Stopwatch();
                watch.Start();
                pool.ProcessTask(task);
                watch.Stop();

                time += watch.ElapsedMilliseconds;
            }

            Console.WriteLine("2 threads: {0} ms.", time);

            time = 0;
            for (int i = 0; i < 100; ++i)
            {
                inputs = GetInputs();
                task = new MergeTaks(inputs);
                pool = new ThreadPool(1);

                System.Diagnostics.Stopwatch watch = new System.Diagnostics.Stopwatch();
                watch.Start();
                pool.ProcessTask(task);
                watch.Stop();

                time += watch.ElapsedMilliseconds;
            }

            Console.WriteLine("1 thread: {0} ms.", time);

            Console.WriteLine("Konec");
            Console.ReadLine();
            //*/
        }

        private static int GetThreadCount(string[] args)
        {
            int threadCount;
            if (args.Length != 1 || !Int32.TryParse(args[0], out threadCount) || threadCount > 256 || threadCount < 1)
            {
                Console.WriteLine("Argument Error");
                return 0;
            }

            return threadCount;
        }

        private static List<int> GetInputs()
        {
            // /*
            List<int> numbers = new List<int>();
            string line;
            while (!String.IsNullOrEmpty((line = Console.ReadLine())))
            {
                int num;
                if (Int32.TryParse(line, out num))
                {
                    numbers.Add(num);
                }
                else
                {
                    Console.WriteLine("Format Error");
                    return null;
                }
            }

            return numbers;
            //*/

            /*
            List<int> numbers = new List<int>() { 
                2, 5, 3, -1, 7, 4, 0, 8, -7, 1, 
                33, 100, 4, 1000, -445, 999, -339, 3000994,
                343443, 334,2232, 2, 232332, 232, 23322, 554, 
                393793, 9839, 9289348, 329, 3875, 87, 
                94738, 743847,  872438, 20389, 0239, 0239,
                2989, 483498, 029, 023909, 20390, 20390, 39039,
                923, 2398, 2398398, 2398, 3498, 9348, 8237,             
                2, 5, 3, -1, 7, 4, 0, 8, -7, 1, 
                33, 100, 4, 1000, -445, 999, -339, 3000994,
                343443, 334,2232, 2, 232332, 232, 23322, 554, 
                393793, 9839, 9289348, 329, 3875, 87, 
                94738, 743847,  872438, 20389, 0239, 0239,
                2989, 483498, 029, 023909, 20390, 20390, 39039,
                923, 2398, 2398398, 2398, 3498, 9348, 8237,             
                2, 5, 3, -1, 7, 4, 0, 8, -7, 1, 
                33, 100, 4, 1000, -445, 999, -339, 3000994,
                343443, 334,2232, 2, 232332, 232, 23322, 554, 
                393793, 9839, 9289348, 329, 3875, 87, 
                94738, 743847,  872438, 20389, 0239, 0239,
                2989, 483498, 029, 023909, 20390, 20390, 39039,
                923, 2398, 2398398, 2398, 3498, 9348, 823,             
                2, 5, 3, -1, 7, 4, 0, 8, -7, 1, 
                33, 100, 4, 1000, -445, 999, -339, 3000994,
                343443, 334,2232, 2, 232332, 232, 23322, 554, 
                393793, 9839, 9289348, 329, 3875, 87, 
                94738, 743847,  872438, 20389, 0239, 0239,
                2989, 483498, 029, 023909, 20390, 20390, 39039,
                923, 2398, 2398398, 2398, 3498, -33, 208};

            return numbers;
            // */
        }

        /// <summary>
        /// Class that provides a single/multi threaded way to solve MergeTask
        /// </summary>
        class ThreadPool
        {
            private int threadsCount = 0;
            private List<Thread> pool;

            /// <summary>
            /// Constructor with maximal number of threads
            /// </summary>
            /// <param name="size"></param>
            public ThreadPool(int size)
            {
                this.threadsCount = size;
            }

            /// <summary>
            /// Merge task to parallelize
            /// </summary>
            /// <param name="task"></param>
            public void ProcessTask(MergeTaks task)
            {
                // Single threaded version
                if (threadsCount == 0)
                {
                    while (!task.Finished)
                    {
                        task.Merge();
                    }
                }
                else
                {
                    // Multi threaded version
                    while (!task.Finished)
                    {
                        this.pool = new List<Thread>(threadsCount);

                        // Run parallely in every allowed new thread
                        for (int i = 0; i < threadsCount; ++i)
                        {
                            var thread = new Thread(() => task.Merge());
                            pool.Add(thread);
                            thread.Start();
                        }

                        // Run even in the main thread
                        task.Merge();

                        // Wait for all threads to finish
                        while (pool.Count(thread => thread.ThreadState == ThreadState.Running) > 0)
                        {
                            // Do not waiste time while waiting
                            task.Merge();
                        }
                    }
                }
            }
        }

        /// <summary>
        /// The class that provides merge sort functionality.
        /// </summary>
        class MergeTaks
        {
            private Queue<List<int>> queue;

            /// <summary>
            /// Indicates whether merge task has finished
            /// </summary>
            public bool Finished
            {
                get
                {
                    lock (queue)
                    {
                        return queue != null && queue.Count == 1;
                    }
                }
            }

            /// <summary>
            /// Deques a list from queue
            /// </summary>
            private List<int> NextList
            {
                get
                {
                    lock (queue)
                    {
                        if (queue.Count > 0)
                        {
                            // queue.Dequeue(); sometimes returns null, I have no idea why
                            var list = queue.Dequeue();
                            return list ?? new List<int>(0);
                        }
                        else
                            return new List<int>(0); ;
                    }
                }
            }

            /// <summary>
            /// Constructor with a numbers to be sorted 
            /// </summary>
            /// <param name="numbers">Numbers to sort</param>
            public MergeTaks(List<int> numbers)
            {
                this.queue = new Queue<List<int>>(numbers.Count);

                foreach (var num in numbers)
                {
                    queue.Enqueue(new List<int>(1) { num });
                }
            }

            /// <summary>
            /// The merge method of the merge task. Dequeues two list to merge, merges them and enqueues the result back to the queue.
            /// </summary>
            public void Merge()
            {
                var merged = new List<int>();
                merged.AddRange(NextList);
                merged.AddRange(NextList);

                // Nothing dequeued
                if (merged.Count == 0)
                    return;

                merged.Sort();

                queue.Enqueue(merged);
            }

            /// <summary>
            /// Prints ordered numbers separately on a line on standard output
            /// </summary>
            public void PrintResult()
            {
                foreach (var num in this.queue.Dequeue())
                {
                    Console.WriteLine(num);
                }
            }

        }
    }
}
