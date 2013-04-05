using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace ParallelFileSearch
{
    /// <summary>
    /// Two phase file queue that collects files to be processed.
    /// Composes of two queues
    ///     - first queue is actual queue that is limited to have a maximal length
    ///     - second is a buffer for the first queue
    ///         - when a file is added and first queue is full, is added to the second queue
    ///         - first queue fills itself automaticaly from the second queue
    /// On the outside acts as a simple queue with no length limit
    /// </summary>
    class FileQueue
    {
        // Second file queue
        Queue<FileInfo> pendingFiles;

        // Firs file queue
        Queue<FileInfo> filesToProcess;

        // Indicates than no more files are comming to be processed
        private bool fileSearchFinished;

        private int FilesToProcessCount { get { lock (filesToProcess) return filesToProcess.Count; } }
        private int PendingFilesCount { get { return pendingFiles.Count; } }

        /// <summary>
        /// Indiacates that queue is empty, which means
        ///     - file search finished and no more files won't be comming
        ///     - no files are in the queue
        /// </summary>
        public bool Empty { get { return fileSearchFinished && PendingFilesCount == 0 && FilesToProcessCount == 0;} }

        int maximumSize;
        public FileQueue(int maximumSize)
        {
            this.maximumSize = maximumSize;
            this.fileSearchFinished = false;

            this.filesToProcess = new Queue<FileInfo>(this.maximumSize);
            this.pendingFiles = new Queue<FileInfo>();
        }

        /// <summary>
        /// Enqueues file to be processed
        /// </summary>
        /// <param name="file">File to be processed</param>
        public void AddFileToProcess(FileInfo file)
        {
            System.Diagnostics.Debug.Assert(!fileSearchFinished, "No more files should be added!");
            pendingFiles.Enqueue(file);
        }

        /// <summary>
        /// Gets file that should be processed. 
        /// Returned NULL value means there is currently no file to process, but it doesn't mean that the queue is empty.
        /// </summary>
        /// <returns>Returns NULL or file to process.</returns>
        public FileInfo GetFileToProcess()
        {
            if (Empty)
                return null;

            // Fill the files queue from pending files queue to maximal allowed count
            lock (filesToProcess)
                while (FilesToProcessCount < maximumSize && PendingFilesCount > 0)
                    filesToProcess.Enqueue(pendingFiles.Dequeue());

            // Get the first file to be processed
            lock (filesToProcess)
            {
                if (FilesToProcessCount == 0)
                    return null;

                return filesToProcess.Dequeue();
            }
        }

        /// <summary>
        /// Needs to be called when no other files won't be added to the queue
        /// </summary>
        public void FileSearchFinished()
        {
            this.fileSearchFinished = true;
        }
    }
}
