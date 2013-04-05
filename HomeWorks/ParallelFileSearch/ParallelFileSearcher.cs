using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Threading;

namespace ParallelFileSearch
{
    class ParallelFileSearcher
    {
        string pattern;
        string path;
        int searcherThreadCount;
        int queueSize;

        int totalFilesBrowsed = 0;

        SearchAutomat automat;
        FileQueue processQueue;
        List<SearchFileTask> taskPool;

        // Represents currently browsed directory layer
        Queue<DirectoryInfo> discoveredFolders = new Queue<DirectoryInfo>();

        // Represents next directory layer to browse
        Queue<DirectoryInfo> pendingDiscoveredFolders = new Queue<DirectoryInfo>();

        public ParallelFileSearchForm SearchForm { get; set; }

        public ParallelFileSearcher(string pattern, string path, int searcherThreadCount, int queueSize)
        {
            this.pattern = pattern;
            this.path = path;
            this.searcherThreadCount = searcherThreadCount;
            this.queueSize = queueSize;

            // Build search automat
            this.automat = new SearchAutomat(pattern);
            this.processQueue = new FileQueue(queueSize);
        }

        public void Run()
        {
            BrowsePathInBackground();

            SearchInFilesInBackground();

            // Wait till all task are finished
            foreach (var task in taskPool)
                task.WaitForEnd();

            // Wait till all tasks (hopefully) starts
            Thread.Sleep(1000);

            foreach (var task in taskPool)
                System.Diagnostics.Debug.Assert(!task.IsRunning, "All tasks should have stopped by now");

            System.Diagnostics.Debug.Assert(this.processQueue.Empty, "There should be no file to process any more.");

            // Tell form simply how many files was totaly browsed
            SearchForm.TotalBrowsedFiles = totalFilesBrowsed;
        }

        /// <summary>
        /// Creates a thread that browses the whole root directory in BFS. 
        /// When finished, notifies processFielQueue that all files discovered.
        /// </summary>
        private void BrowsePathInBackground()
        {
            // Asynchronously browse the whole directory structure
            new Thread(
                () =>
                {
                    BrowseDirectoryBFS(new DirectoryInfo(this.path));
                    this.processQueue.FileSearchFinished();
                }
            ).Start();
        }

        /// <summary>
        /// Browses the whole files and directories structure from the current path in BFS order.
        /// </summary>
        /// <param name="rootDirInfo">The root directory to browse</param>
        private void BrowseDirectoryBFS(DirectoryInfo rootDirInfo)
        {
            // Add files to the queue
            foreach (var fileInfo in rootDirInfo.GetFiles())
            {
                this.processQueue.AddFileToProcess(fileInfo);
                ++totalFilesBrowsed;
            }

            // Add directories to the next layer
            foreach (var directoryInfo in rootDirInfo.GetDirectories())
            {
                this.pendingDiscoveredFolders.Enqueue(directoryInfo);
            }

            // Current layer processed, move to the next one
            if (this.discoveredFolders.Count == 0)
            {
                // All directories processed
                if (this.pendingDiscoveredFolders.Count == 0)
                    return;

                // Switch next layer as current layer and start processing it
                this.discoveredFolders = this.pendingDiscoveredFolders;
                this.pendingDiscoveredFolders = new Queue<DirectoryInfo>();
            }

            // Recursive call on the next directory in order
            BrowseDirectoryBFS(this.discoveredFolders.Dequeue());
        }
        
        private void SearchInFilesInBackground()
        {
            this.taskPool = new List<SearchFileTask>(this.searcherThreadCount);

            // Create maximum of allowed tasks (task <=> thread) and trigger it
            for (int i = 0; i < this.searcherThreadCount; ++i)
            {
                var task = new SearchFileTask(this.processQueue, this.automat, this.SearchForm);
                taskPool.Add(task);
                task.Run();
            }
        }
    }
}
