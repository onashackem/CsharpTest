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
        private SortedSet<string> acceptedFiles = new SortedSet<string>();
        private bool searching = false;

        public ParallelFileSearchForm(ParallelFileSearcher searcher)
        {
            this.searcher = searcher;
            this.searcher.SearchForm = this;

            InitializeComponent();
        }

        public void AcceptFile(FileInfo file)
        {
            this.acceptedFiles.Add(file.Name);
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
            this.lbx_Files.Items.Clear();
            this.lbx_Files.Items.AddRange(acceptedFiles.ToArray());

            UpdateStats();
        }

        private void UpdateStats()
        {
            int filesProcessed = this.acceptedFiles.Count;

            this.label1.Text = String.Format(
                "{0} {1} files containing pattern found.",
                (searching) ? "Searching in progress, so far" : "Searching finished.",
                filesProcessed
            );

        }

        private void ParallelFileSearchForm_Load(object sender, EventArgs e)
        {
            // Trigger searching
            new System.Threading.Thread(()=>this.searcher.Run());
        }
    }
}
