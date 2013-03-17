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
        bool directoriesBrowsed = false;

        SearchAutomat automat;
        FileQueue processQueue;
        public ParallelFileSearchForm SearchForm { get; set; }

        // Represents currently browsed directory layer
        Queue<DirectoryInfo> discoveredFolders = new Queue<DirectoryInfo>();

        // Represents next directory layer to browse
        Queue<DirectoryInfo> pendingDiscoveredFolders = new Queue<DirectoryInfo>();

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
            this.SearchForm.OnSearchStarted();

            BrowsePathInBackground();

            SearchInFiles();
        }

        private void BrowsePathInBackground()
        {
            new Thread(
                () =>
                {
                    // Asynchronously browse the whole directory structure
                    BrowseDirectory(new DirectoryInfo(this.path));
                    this.processQueue.FileSearchFinished();
                }
            ).Start();
        }

        /// <summary>
        /// Browses the whole files and directories structure from the current path in BFS order.
        /// </summary>
        /// <param name="rootDirInfo">The root directory to browse</param>
        private void BrowseDirectory(DirectoryInfo rootDirInfo)
        {
            // Add files to the queue
            foreach (var fileInfo in rootDirInfo.GetFiles())
            {
                this.processQueue.AddFileToProcess(fileInfo);
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
                {
                    // Notify about the end of search
                    //TODO: this.processQueue.FileSearchFinished();
                    return;
                }

                // Switch next layer as current layer and start processing it
                this.discoveredFolders = this.pendingDiscoveredFolders;
                this.pendingDiscoveredFolders = new Queue<DirectoryInfo>();
            }

            // Recursive call on the next directory in order
            BrowseDirectory(this.discoveredFolders.Dequeue());
        }

        private void SearchInFiles()
        {
            FileInfo fileToProcess = null;

            while ((fileToProcess = this.processQueue.GetFileToProcess()) != null)
            {
                var fileSearcher = new FileSearcher(fileToProcess);

                if (fileSearcher.IsMatch(this.automat.GetInitilState()))
                {
                    this.SearchForm.AcceptFile(fileToProcess);
                }
            }

            this.SearchForm.OnSearchFinished();
        }

        private class SearchFileTask
        {
            private Thread thread;
            private FileQueue processQueue;
            private SearchAutomat automat;
            private ParallelFileSearchForm form;

            public SearchFileTask(FileQueue processQueue, SearchAutomat automat, ParallelFileSearchForm form)
            {
                this.processQueue = processQueue;
                this.automat = automat;
                this.form = form;
            }

            public void Run()
            {
                while (!this.processQueue.Empty)
                {
                    FileInfo fileToProcess = this.processQueue.GetFileToProcess();

                    if (fileToProcess == null)
                    {
                        Thread.Sleep(100);
                        break;
                    }
                    
                    thread = new Thread(() => SearchFile(fileToProcess));
                    thread.Start();
                    thread.Join();
                }
            }

            private void SearchFile(FileInfo fileToProcess)
            {
                var fileSearcher = new FileSearcher(fileToProcess);

                if (fileSearcher.IsMatch(this.automat.GetInitilState()))
                {
                    this.form.AcceptFile(fileToProcess);
                }
            }

            public bool IsRunning()
            {
                return thread.ThreadState == ThreadState.Running;
            }
        }
    }
}
