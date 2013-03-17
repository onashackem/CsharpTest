using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Cuni.NPrg038;

namespace ParallelFileSearch
{
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
}
