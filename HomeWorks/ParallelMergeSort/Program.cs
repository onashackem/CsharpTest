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
            var poolSize = GetPoolSize(args);
            if (poolSize < 1)
                return;

            // Read numbers to sort
            var inputs = GetInputs();
            if (inputs == null)
                return;

            // No sence to parallelize
            if (inputs.Count < 2 * poolSize)
                poolSize = 1;

            var task = new MergeTaks(inputs);
            var pool = new ThreadPool(poolSize);

            pool.ProcessTask(task);

            task.PrintResult();

            ///* TEST
            long time = 0;
            for (int i = 0; i < 50; ++i)
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
            for (int i = 0; i < 50; ++i)
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
         
        private static int GetPoolSize(string[] args)
        {
            int threadCount;
            if (args.Length != 1 || !Int32.TryParse(args[0], out threadCount) || threadCount > 256 || threadCount < 1)
            {
                Console.WriteLine("Argument Error");
                return 0;
            }

            return threadCount;
        }

        private static Queue<List<int>> GetInputs()
        {
            Queue<List<int>> inputs = new Queue<List<int>>();

            /*
            string line;
            while(!String.IsNullOrEmpty((line = Console.ReadLine())))
            {
                int num;
                if (Int32.TryParse(line, out num))
                {
                    inputs.Enqueue(new List<int>() { num });
                }
                else
                {
                    Console.WriteLine("Format Error");
                    return null;
                }
            }

            //return inputs;
            */

            ///*
            List<int> numbers = new List<int>() { 
                2, 5, 3, -1, 7, 4, 0, 8, -7, 1, 
                33, 100, 4, 1000, -445, 999, -339, 3000994,
                343443, 334,2232, 2, 232332, 232, 23322, 554, 
                393793, 9839, 9289348, 329, 3875, 87, 
                94738, 743847,  872438, 20389, 0239, 0239,
                2989, 483498, 029, 023909, 20390, 20390, 39039,
                923, 2398, 2398398, 2398, 3498, 9348, 8237};
            foreach (var num in numbers)
            {
                inputs.Enqueue(new List<int>() { num });
            }

            return inputs;
            // */
        }

        class ThreadPool
        {
            private int threadsCount = 0;
            private List<Thread> pool;

            public ThreadPool(int size)
            {
                this.threadsCount = size - 1;

                // Parallelize only when creating any new thread is allowed
                if (threadsCount > 0)
                {
                    this.pool = new List<Thread>(threadsCount);
                }
            }

            public void ProcessTask(MergeTaks task)
            {
                while (!task.Finished)
                {
                    if (threadsCount == 0)
                    {
                        task.Merge(task.NextList, task.NextList);
                    }
                    else
                    {
                        for(int i = 0; i < threadsCount; ++i)
                        {
                            //var thread = new Thread(task.Merge);
                            var thread = new Thread(() => task.Merge(task.NextList, task.NextList));
                            pool.Add(thread);
                            thread.Start();
                        }

                        // Do also some work during waiting
                        task.Merge(task.NextList, task.NextList);

                        foreach (var thread in pool)
                        {
                            thread.Join();
                        }

                        //System.Diagnostics.Debug.Assert(pool.Count(thread => thread.ThreadState == ThreadState.Stopped) == threadsCount, "Not all threads finished");
                            
                        // Wait for all threads to finish
                        while (pool.Count(thread => thread.ThreadState == ThreadState.Stopped) < threadsCount)
                        {
                            // Do also some work during waiting
                            task.Merge(task.NextList, task.NextList);
                        }
                    }
                }
            }
        }

        class MergeTaks
        {
            Queue<List<int>> queue;

            public bool Finished { get { lock (queue) { return queue != null && queue.Count == 1; } } }

            public List<int> NextList { get { if (queue.Count > 0) return queue.Dequeue(); else return null; } }

            public MergeTaks(Queue<List<int>> inputs)
            {
                this.queue = inputs;
            }

            public void Merge(List<int> list1, List<int> list2)
            {
                //Console.WriteLine("Current thread: " + Thread.CurrentThread.ManagedThreadId);   

                if (list1 == null && list2 == null)
                    return;

                if (list1 == null)
                {
                    queue.Enqueue(list2);
                    return;
                }

                if (list2 == null)
                {
                    queue.Enqueue(list1);
                    return;
                }

                var merged = new List<int>(list1.Count + list2.Count);
                merged.AddRange(list1);
                merged.AddRange(list2);
                merged.Sort();

                queue.Enqueue(merged);

                //Console.WriteLine("Thread: " + Thread.CurrentThread.ManagedThreadId + " finished.");
            }

            public void PrintResult()
            {
                System.Diagnostics.Debug.Assert(queue.Count == 1, "Not finished");

                foreach (var num in this.queue.Dequeue())
                {
                    Console.WriteLine(num);
                }
            }

        }
    }
}
