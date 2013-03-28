using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace ParallelTaskSort
{
    class Program
    {
        private static readonly int BUFFER_SIZE = 10;
        private static readonly Queue<List<int>> sortedListsQueue = new Queue<List<int>>();
        int levelsCount = -1;
        int lastCompletedLevel = -1;

        /*
         * Maximal parallelization of MERGE tasks
         * Using System.Threading.Tasks.*
         * Do not use Thread, ThreadPool
         * Task(<T>).Wait, WaitALL, WaitAny, ContinueWith <==> TaskFactory.ContinueWithAll/Any(b)
         * 
         * Read 10kB block of intput, then Array.Sort while reading another 10kB input block
         * When 2 blocks sorted, merge them, etc. 
         * While merging 2 last bits, and some part is already merged, can send to output while still merging the rest of those 2 bits
         * 
         * program.exe numbers.in sorted.out
         */
        static void Main(string[] args)
        {
            List<int> buffer1 = new List<int>(10) {5, 9, 3, 6, 0, 7, 8};
            List<int> buffer2 = new List<int>(10) {15, 6, 4, 51, 1, 18, 91, -8};

            new SortTaskRunner(0, buffer1, buffer2);

            Task print = new Task(() => Print(Console.Out, sortedListsQueue.Dequeue()));
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

        static void Print(TextWriter writer, List<int> numbers)
        {
            foreach (var n in numbers)
            {
                writer.WriteLine(n);
            }
        }
    }

    class Merger
    {
        private readonly Dictionary<int, SortTasksLevel> levels = new Dictionary<int,SortTasksLevel>();
        private readonly Dictionary<int, SortTaskRunner> finishedTasks = new Dictionary<int, SortTaskRunner>();

        public void AddSortTask(SortTaskRunner task)
        {
            lock(levels) 
            {
                if (!levels.ContainsKey(task.Level))
                    levels.Add(task.Level, new SortTasksLevel());
            }

            levels[task.Level].AddTaskRunner(task);
        }

        public void TaskCompleted(SortTaskRunner task)
        {
            lock (finishedTasks)
            {
                if (finishedTasks.ContainsKey(task.Level))
                {
                    var pendingTask = finishedTasks[task.Level];
                    finishedTasks.Remove(task.Level);
                    AddSortTask(new SortTaskRunner(task.Level + 1, pendingTask.Result, task.Result));
                }
                else
                {
                    finishedTasks.Add(task.Level, task);
                }
            }
        }
    }

    class SortTasksLevel
    {
        public List<SortTaskRunner> Tasks { get; private set; }

        public SortTasksLevel()
        {
            Tasks = new List<SortTaskRunner>();
        }

        private int schedulledTasksCount = -1;
        private bool allTasksSchedulled = false;
        public bool IsLevelCompleted { get { return (allTasksSchedulled || schedulledTasksCount == Tasks.Count) && Tasks.Count(t => !t.IsTaskCompleted) == 0;  } }

        public void AddTaskRunner(SortTaskRunner taskRunner)
        {
            Tasks.Add(taskRunner);
        }

        public void SetAllTasksSchedulled()
        {
            this.allTasksSchedulled = true;
        }

        public void SetExpectedSchedulledTaksCount(int count)
        {
            this.schedulledTasksCount = count;
        }
    }


    class SortTaskRunner
    {
        public int Level { get; private set; }

        public Task<List<int>> Task { get; private set; }

        public List<int> Result { get { return Task.Result; } }

        public bool IsTaskCompleted { get { return Task.IsCompleted; } }
                
        public SortTaskRunner(int level, List<int> list1, List<int> list2)
        {
            this.Level = level;

            this.Task = new Task<List<int>>(() => Sort(list1, list2));
            Task.ContinueWith(task => { });
            Task.Start();
        }

        private List<int> Sort(List<int> list1, List<int> list2)
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
