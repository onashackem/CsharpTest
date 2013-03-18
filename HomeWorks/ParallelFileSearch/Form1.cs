using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.IO;

namespace ParallelFileSearch
{
    /// <summary>
    /// Windows form that displays all files that contains searched pattern
    /// </summary>
    partial class ParallelFileSearchForm : Form
    {
        private ParallelFileSearcher searcher;
        private List<FileInfo> acceptedFiles = new List<FileInfo>();
        private bool searching = false;
        
        /// <summary>
        /// Indicates how many files was totaly browsed.
        /// Now just set when search is finished
        /// </summary>
        public int TotalBrowsedFiles { get; set; }

        /// <summary>
        /// Creates form to show found files
        /// </summary>
        /// <param name="searcher">The whole searching logic</param>
        public ParallelFileSearchForm(ParallelFileSearcher searcher)
        {
            InitializeComponent();

            this.searcher = searcher;
            this.searcher.SearchForm = this;
            this.timer.Start();
        }

        /// <summary>
        /// Called when file containing pattern found. Adds file to the output list
        /// </summary>
        /// <param name="file">File containing pattern</param>
        public void AcceptFile(FileInfo file)
        {
            lock (acceptedFiles) 
                this.acceptedFiles.Add(file);
        }

        private void OnSearchStarted()
        {
            this.searching = true;

            UpdateStats();
        }

        private void OnSearchFinished()
        {
            this.searching = false;
            this.timer.Stop();

            UpdateStats();
        }

        private void timer_Tick(object sender, EventArgs e)
        {
            lock (acceptedFiles)
            {
                this.lbx_Files.Items.AddRange(acceptedFiles.ToArray());
                acceptedFiles.Clear();
            }

            UpdateStats();
        }

        private void UpdateStats()
        {
            int filesProcessed = this.lbx_Files.Items.Count;

            if (InvokeRequired)
                Invoke((Action)( () => { UpdateStats_Internal(filesProcessed); }));
            else
                UpdateStats_Internal(filesProcessed);

        }

        private void UpdateStats_Internal(int filesProcessed)
        {
            this.lbl_Stats.Text = String.Format(
                "{0} {1} files containing pattern found.",
                (searching) ? "Searching in progress, so far" : "Searching finished.",
                (searching) ? filesProcessed.ToString() : String.Format("{0}/{1}", filesProcessed, TotalBrowsedFiles)
            );
        }

        private void ParallelFileSearchForm_Load(object sender, EventArgs e)
        {
            // Trigger searching
            new System.Threading.Thread(
                ()=>
                {
                    OnSearchStarted();

                    // Ends when searching all files finished
                    this.searcher.Run();

                    OnSearchFinished();
                }
            ).Start();
        }

        /// <summary>
        /// On closed action that terminates the whole application.
        /// </summary>
        /// <param name="e"></param>
        protected override void OnClosed(EventArgs e)
        {
            base.OnClosed(e);

            timer.Stop();

            // Kill app and all searching threads
            Environment.Exit(0);
        }
    }
}
