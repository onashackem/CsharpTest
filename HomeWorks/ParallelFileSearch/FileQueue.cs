using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace ParallelFileSearch
{
    class FileQueue
    {
        Queue<FileInfo> pendingFiles;
        Queue<FileInfo> filesToProcess;
        private bool fileSearchFinished;

        private int FilesToProcessCount { get { lock (filesToProcess) return filesToProcess.Count; } }
        private int PendingFilesCount { get { return pendingFiles.Count; } }

        public bool Empty { get { return fileSearchFinished && PendingFilesCount == 0; } }

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
            if (Empty)
                return null;

            // Fill the files queue from with pending files queue
            while (FilesToProcessCount < maximumSize && PendingFilesCount > 0)
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
