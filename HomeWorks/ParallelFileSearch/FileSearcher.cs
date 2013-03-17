using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using Cuni.NPrg038;

namespace ParallelFileSearch
{
    /// <summary>
    /// Determintes whether file contains searched pattern.
    /// </summary>
    class FileSearcher
    {
        private readonly int BUFFER_SIZE = 2000000;
        private FileInfo fileInfo;
        private byte[] buffer;

        public FileSearcher(FileInfo fileInfo)
        {
            this.fileInfo = fileInfo;
            buffer = new byte[BUFFER_SIZE];
        }

        /// <summary>
        /// Reads the given file using defined state of a search automat.
        /// </summary>
        /// <param name="initialSearchState">Initial state of a search automat</param>
        /// <returns>Returns true if automat finds pattern match</returns>
        public bool IsMatch(IByteSearchState initialSearchState)
        {
            // Read file (part by part) into buffer
            var state = initialSearchState;
            using (FileStream input = new FileStream(this.fileInfo.FullName, FileMode.Open))
            {
                // Read the BUFFER_SIZE long part of file
                int bytesRead = -1;
                while (0 != (bytesRead = input.Read(buffer, 0, BUFFER_SIZE)))
                {
                    // Process every byte in buffer
                    for (int index = 0; index < bytesRead; ++index)
                    {
                        // Process byte by byte
                        state = state.GetNextState(buffer[index]);

                        // Pattern found
                        if (state.HasMatchedPattern)
                            return true;
                    }
                }
            }

            // Not found
            return false;
        }
    }
}
