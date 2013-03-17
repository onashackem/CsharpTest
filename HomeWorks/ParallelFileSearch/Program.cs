using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using System.IO;
using Cuni.NPrg038;
using System.Text;
using System.Threading;

namespace ParallelFileSearch
{
    /*
     * program.exe pattern_to_search path_to_browe searcher_thread_count queue_depth
     * 
     * - Browse path in DFS anf list all files that contains pattern_to_search (use extern AC algorithm)
     *      - create automat once and get the initial state for every file
     *      - use FileStream and read byte by byte to and use automat states (read into large buffers! 16MB is OK)
     *      - ASCII, UTF-8 and UTF-16 encodings -> three patterns to search
     * - Put files into shared queue of maximal length of queue_depth
     * - Use maximal number of searcher_thread_count to browse files
     * - Allowed: Monitor, SemaphoreSlim, Countdownevent, Thread
     * - Output 
     *      - WinForms application with ListBox with ordered!! files containing pattern
     *      - Number of found files (number of browsed files, unreadable files)
     *      - reasonable exit (X button) behavior
     */
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main(string[] args)
        {
            // TODO: params check - "Argument error" and exit;
            args = new string[] { "=chyba", @"c:\Temp", "4", "16" };

            string pattern = args[0];
            string path = args[1];
            int searcherThreadCount = Int32.Parse(args[2]);
            int queueSize = Int32.Parse(args[3]);

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(
                new ParallelFileSearchForm(
                    new ParallelFileSearcher(pattern, path, searcherThreadCount, queueSize)
                )
            );            
        }
    }

    class ParallelFileSearcher
    {
        string pattern;
        string path;
        int searcherThreadCount;
        int queueSize;

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

            // Asynchronously browse the whole directory structure
            BrowseDirectory(new DirectoryInfo(this.path));

            SearchInFiles();

            this.SearchForm.OnSearchFinished();
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
        }
    }

    /// <summary>
    /// Determintes whether file contains searched pattern.
    /// </summary>
    class FileSearcher
    {
        private readonly int BUFFER_SIZE = 2000000;
        private FileInfo fileInfo;
        private byte[] buffer;

        public FileSearcher(FileInfo fileInfo)
        {
            this.fileInfo = fileInfo;
            buffer = new byte[BUFFER_SIZE];
        }

        /// <summary>
        /// Reads the given file using defined state of a search automat.
        /// </summary>
        /// <param name="initialSearchState">Initial state of a search automat</param>
        /// <returns>Returns true if automat finds pattern match</returns>
        public bool IsMatch(IByteSearchState initialSearchState)
        {
            // Read file (part by part) into buffer
            var state = initialSearchState;
            using (FileStream input = new FileStream(this.fileInfo.FullName, FileMode.Open))
            {
                // Read the BUFFER_SIZE long part of file
                int bytesRead = -1;
                while (0 != (bytesRead = input.Read(buffer, 0, BUFFER_SIZE)))
                {
                    // Process every byte in buffer
                    for (int index = 0; index < bytesRead; ++index)
                    {
                        // Process byte by byte
                        state = state.GetNextState(buffer[index]);

                        // Pattern found
                        if (state.HasMatchedPattern)
                            return true;
                    }
                }
            }

            // Not found
            return false;
        }

    }

    class SearchAutomat
    {
        AhoCorasickSearch automat = new AhoCorasickSearch();

        public SearchAutomat(string pattern)
        {
            automat.AddPattern(Encoding.ASCII.GetBytes(pattern));
            automat.AddPattern(Encoding.UTF8.GetBytes(pattern));
            automat.AddPattern(Encoding.Unicode.GetBytes(pattern));
            automat.Freeze();
        }

        public IByteSearchState GetInitilState()
        {
            return automat.InitialState;
        }

    }

    class FileQueue
    {
        Queue<FileInfo> pendingFiles;
        Queue<FileInfo> filesToProcess;
        private bool fileSearchFinished;

        private int FilesToProcessCount { get { lock (filesToProcess) return filesToProcess.Count; } }
        private int PendingFilesCount { get { return pendingFiles.Count; } }

        int maximumSize;
        public FileQueue(int maximumSize)
        {
            this.maximumSize = maximumSize;
            this.fileSearchFinished = false;

            this.filesToProcess = new Queue<FileInfo>(this.maximumSize);
            this.pendingFiles = new Queue<FileInfo>();
        }

        public void AddFileToProcess(FileInfo file)
        {
            pendingFiles.Enqueue(file);
        }

        public FileInfo GetFileToProcess()
        {
            // Fill the files queue from with pending files queue
            while (!fileSearchFinished && FilesToProcessCount < maximumSize && PendingFilesCount > 0)
            {
                lock (filesToProcess)
                    filesToProcess.Enqueue(pendingFiles.Dequeue());
            }

            // Get the first file to be processed
            lock (filesToProcess)
            {
                if (FilesToProcessCount == 0)
                    return null;

                return filesToProcess.Dequeue();
            }
        }

        public void FileSearchFinished()
        {
            this.fileSearchFinished = true;
        }
    }
}
