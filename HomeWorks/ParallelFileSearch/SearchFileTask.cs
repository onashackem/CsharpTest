using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.IO;

namespace ParallelFileSearch
{
    class SearchFileTask
    {
        private Thread thread;
        private FileQueue processQueue;
        private SearchAutomat automat;
        private ParallelFileSearchForm form;

        /// <summary>
        /// Creates a new instance of SearchFileTask.
        /// </summary>
        /// <param name="processQueue">Queue containing files to seach pattern in</param>
        /// <param name="automat">Automat that searches pattern</param>
        /// <param name="form">Form that is a file match reported to</param>
        public SearchFileTask(FileQueue processQueue, SearchAutomat automat, ParallelFileSearchForm form)
        {
            this.processQueue = processQueue;
            this.automat = automat;
            this.form = form;
        }

        /// <summary>
        /// Triggers the search task, that
        ///     - ask for a file to process
        ///     - searched that file for pattern
        ///     - reports when pattern in file found
        /// </summary>
        public void Run()
        {
            thread = new Thread(() => 
                {
                    // While there is a file t process
                    while (!this.processQueue.Empty)
                    {
                        // Ask for a next file
                        FileInfo fileToProcess = this.processQueue.GetFileToProcess();

                        // There is a file to process
                        if (fileToProcess != null)
                        {
                            SearchFile(fileToProcess);
                        }
                        else
                        {
                            // Monitor.Wait()?
                            Thread.Sleep(100);
                        }

                    }
                }
            );
            
            thread.Start();
        }

        private void SearchFile(FileInfo fileToProcess)
        {
            // If pattern is found in file, send file to form
            if (new FileSearcher(this.automat.GetInitilState()).IsMatch(fileToProcess))
            {
                this.form.AcceptFile(fileToProcess);
            }
        }

        /// <summary>
        /// Indicates whether this search task is running
        /// </summary>
        public bool IsRunning
        {
            get { return thread != null && thread.ThreadState == ThreadState.Running; }
        }

        /// <summary>
        /// Waits till the task has finished
        /// </summary>
        public void WaitForEnd()
        {
            this.thread.Join();
        }
    }
}
