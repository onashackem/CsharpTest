using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Cuni.NPrg038;

namespace ParallelFileSearch
{
    /// <summary>
    /// Automat that searches pattern (in 3 encodings) in files
    /// </summary>
    class SearchAutomat
    {
        AhoCorasickSearch automat = new AhoCorasickSearch();

        /// <summary>
        /// Builds search automat to search pattern in ASCII, UTF8, UTF16 (Unicode)
        /// </summary>
        /// <param name="pattern"></param>
        public SearchAutomat(string pattern)
        {
            automat.AddPattern(Encoding.ASCII.GetBytes(pattern));
            automat.AddPattern(Encoding.UTF8.GetBytes(pattern));
            automat.AddPattern(Encoding.Unicode.GetBytes(pattern));
            automat.Freeze();
        }

        /// <summary>
        /// Gets the new instance of the initial searching state
        /// </summary>
        /// <returns></returns>
        public IByteSearchState GetInitilState()
        {
            return automat.InitialState;
        }
    }
}
