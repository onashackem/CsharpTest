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
            args = new string[] { "29", @"c:\Temp", "4", "16" };

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
}
