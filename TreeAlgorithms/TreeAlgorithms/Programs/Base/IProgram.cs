using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TreeAlgorithms.Programs.Base
{
    interface IProgram
    {
        /// <summary>
        /// The main method of every algorithm
        /// </summary>
        /// <param name="args">Parameters for a program</param>
        void run(string[] args);
    }
}
