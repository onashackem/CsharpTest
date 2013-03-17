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
    partial class ParallelFileSearchForm : Form
    {
        private ParallelFileSearcher searcher;
        private List<FileInfo> acceptedFiles = new List<FileInfo>();
        private bool searching = false;

        public ParallelFileSearchForm(ParallelFileSearcher searcher)
        {
            InitializeComponent();

            this.searcher = searcher;
            this.searcher.SearchForm = this;
            this.timer.Start();
        }

        public void AcceptFile(FileInfo file)
        {
            this.acceptedFiles.Add(file);
        }

        public void OnSearchStarted()
        {
            this.searching = true;
            UpdateStats();
        }

        public void OnSearchFinished()
        {
            this.searching = false;
            UpdateStats();
        }

        private void timer_Tick(object sender, EventArgs e)
        {
            /*
            this.lbx_Files.BeginUpdate();

            lock (acceptedFiles)
            {*/
                this.lbx_Files.Items.AddRange(acceptedFiles.ToArray());
                acceptedFiles.Clear();
            /*}

            this.lbx_Files.EndUpdate();*/

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
                filesProcessed
            );
        }

        private void ParallelFileSearchForm_Load(object sender, EventArgs e)
        {
            // Trigger searching
            new System.Threading.Thread(()=>this.searcher.Run()).Start();
        }

        protected override void OnClosed(EventArgs e)
        {
            base.OnClosed(e);

            timer.Stop();

            // Kill app and all threads
            Environment.Exit(0);
        }
    }
}
