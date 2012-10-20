using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using CodEx;

namespace WordWrap
{
    class WordWrapper
    {        
        static void Main(string[] args)
        {
            int LINE_LENGTH;
            string INPUT_FILE;
            string OUTPUT_FILE;

            try
            {
                // Read parameters from command line
                if (!ProcessParameters(args, out INPUT_FILE, out OUTPUT_FILE, out LINE_LENGTH))
                    throw new ArgumentException();

                // TODO: remove
                for (int i = 1; i <= LINE_LENGTH; ++i)
                    Console.Write(i%10);
                Console.WriteLine("");

                using (StreamReader reader = new StreamReader(INPUT_FILE))
                using (StreamWriter writer = new StreamWriter(OUTPUT_FILE, false))
                {
                    LineProcessor lineProcessor = new LineProcessor(LINE_LENGTH, writer);
                    WordStreamedReader wordReader = new WordStreamedReader(reader);
                    
                    // Skip the first new line(s) if any
                    string word = wordReader.NextWord();
                    if (word != null && word.Replace("\n", "").Length > 0)
                        lineProcessor.AddWord(word);
                    
                    // Read words untill it overflows the line or the end of stream is reached
                    while (((word = wordReader.NextWord()) != null))
                    {
                        lineProcessor.AddWord(word);
                    }

                    writer.Flush();
                }
            }
            catch (Exception ex)
            {
                if (ex is ArgumentException ||
                    ex is FormatException ||
                    ex is OverflowException)
                {
                    Console.WriteLine("Argument Error");
                }
                else
                    Console.WriteLine("File Error");
            }
        }

        private static bool ProcessParameters(string[] args, out string inputFile, out string outputFile, out int lineLenght)
        {
            inputFile = "";
            outputFile = "";
            lineLenght = 0;

            if (args.Length != 3)
                return false;

            inputFile = args[0];
            outputFile = args[1];
            lineLenght = Convert.ToInt32(args[2]);

            return true;
        }
    }

    /// <summary>
    /// Processes words to fit the line
    /// </summary>
    class LineProcessor
    {
        private readonly int maximalLineLength;
        private int currentLineWordsLenght = 0;
        StreamWriter writer;

        /// <summary>
        /// Sum of lenthg of all words in the current line + 1 space between each two following words 
        /// </summary>
        private int CurrentLineLength 
        {
            get
            {
                return currentLineWordsLenght + Math.Max(0, CurrentLineWords.Count - 1);
            }
        }

        /// <summary>
        /// The list of words in the current line
        /// </summary>
        private List<string> CurrentLineWords { get; set; }

        /// <summary>
        /// .ctor
        /// </summary>
        /// <param name="maxLenght">Maximal length of the line</param>
        /// <param name="writer">Writer to write wrapped line into</param>
        public LineProcessor(int maxLenght, StreamWriter writer)
        {
            this.maximalLineLength = maxLenght;
            this.writer = writer;
            this.CurrentLineWords = new List<string>();
        }

        /// <summary>
        /// Adds word to the current line
        /// </summary>
        /// <param name="word">Word to add</param>
        public void AddWord(string word)
        {
            // Skip empty words (more spaces in a row)
            if (word.Length > 0)
            {
                bool endOfTheParagraph = word.Contains('\n');

                // Remove the newlines from the end of the word
                if (endOfTheParagraph)
                {
                    word = word.Replace("\n", "");
                }

                // New word doesn't fit the line -> print the line without it
                if (word.Length + CurrentLineLength + 1 > this.maximalLineLength)
                {
                    PrintLine();
                }

                // Add word to a line
                CurrentLineWords.Add(word);
                currentLineWordsLenght += word.Length;

                // Is this the end of a paragraph?
                if (endOfTheParagraph)
                {
                    PrintLine(endOfTheParagraph);
                }
            }
        }

        /// <summary>
        /// Prints current line and clears the collection of words in the current line
        /// </summary>
        /// <param name="isEndOfParagraph">Is the end of the paragraph?</param>
        private void PrintLine(bool isEndOfParagraph = false)
        {
            /* Compute spaces between words which depends on:
                *  a) we are at the end of a paragraph
                *      - all two following words are separated with just one space
                *  b) the words neds to be separated equaly (from the left)
                *  c) there could be just 1 word on the line (too long for a line)
                */
            int totalSpaceAvalilable = this.maximalLineLength - CurrentLineWords.Sum(word => word.Length);
            int spacesBetweenWords = isEndOfParagraph ? 1 : (CurrentLineWords.Count <= 1 ? 0 : totalSpaceAvalilable / (CurrentLineWords.Count - 1));
            int spacesLeft = isEndOfParagraph ? 0 : (CurrentLineWords.Count <= 1 ? 0 : totalSpaceAvalilable % (CurrentLineWords.Count - 1));
            
            StringBuilder line = new StringBuilder();

            // Print words with proper spacing
            for (int wordIndex = 0; wordIndex < CurrentLineWords.Count; ++wordIndex )
            {
                line.Append(CurrentLineWords[wordIndex]);

                // No spaces after the last word
                if (wordIndex == CurrentLineWords.Count - 1)
                    break;

                // Spaces between each 2 following words
                int spacesUsed = 0;
                while (++spacesUsed <= spacesBetweenWords)
                {
                    line.Append(" ");
                }

                // Additional spaces from left
                if (spacesLeft > 0)
                {
                    line.Append(" ");
                    --spacesLeft;
                }
            }

            string lineToPrint = line.ToString();

            // Assert full-line length
            if (!isEndOfParagraph && CurrentLineWords.Count > 1)
                System.Diagnostics.Debug.Assert(lineToPrint.Length == this.maximalLineLength);

            // Write computed line
            // TODO: Remove
            Console.Write(lineToPrint);
            Console.WriteLine(string.Format(" {0} {1}", totalSpaceAvalilable, spacesBetweenWords));
            writer.WriteLine(lineToPrint);

            // Clear line after is printed
            CurrentLineWords.Clear();
            currentLineWordsLenght = 0;
        }
    }

    class WordStreamedReader
    {
        StreamReader reader;
        int bufferSize = 10;
        int index = 0;
        int charsRead = 0;

        char[] buffer;

        public WordStreamedReader(StreamReader reader)
        {
            buffer = new char[bufferSize];
            this.reader = reader;

            // Init the buffer
            ReadBuffer();
        }

        public string NextWord()
        {
            if (charsRead == 0)
                return null;

            StringBuilder word = new StringBuilder();

            // Read word
            while (!SkipWhiteSpace(buffer[index]))
            {
                word.Append(buffer[index]);
                ++index;

                if (index == charsRead)
                {
                    ReadBuffer();
                    if (charsRead == 0)
                    {
                        // EOF
                        word.Append("\n");
                        return word.ToString();
                    }
                }
            }

            // Skip white spaces, remember newlines
            bool newLineFound = false;
            while(SkipWhiteSpace(buffer[index]))
            {
                newLineFound |= IsNewline(buffer[index]);
                ++index;

                if (index == charsRead)
                    ReadBuffer();
            }

            // Newline or EOF
            if (newLineFound || charsRead == 0)
                word.Append("\n");

            return word.ToString();
        }

        private void ReadBuffer()
        {
            index = 0;
            charsRead = reader.Read(buffer, index, bufferSize);
        }

        private bool IsWhiteSpace(char c)
        {
            return c == ' ' || c == '\t';
        }

        private bool IsNewline(char c)
        {
            return c == '\n';
        }

        private bool SkipWhiteSpace(char c)
        {
            return IsWhiteSpace(c) || IsNewline(c);
        }
    }

    /// <summary>
    /// Reads word from C# input
    /// </summary>
    class WordReader
    {
        Reader reader;

        char? lookoutChar;

        /// <summary>
        /// Gets whether reader has reached the end of the stream
        /// </summary>
        public bool EndOfStream
        {
            get
            {
                return reader.EOF();
            }
        }

        /// <summary>
        /// .ctor
        /// </summary>
        /// <param name="reader">Input reader</param>
        public WordReader(Reader reader)
        {
            this.reader = reader;
            lookoutChar = null;
        }

        /// <summary>
        /// Reads next word from input
        /// </summary>
        /// <returns>Returns word or null if the end of stream is found</returns>
        public string NextWord()
        {
            if (EndOfStream)
                return null;

            bool whiteSpaceFound = false;
            StringBuilder word = new StringBuilder();

            // Append char read in previous method call
            if (lookoutChar.HasValue)
                word.Append(lookoutChar.Value);

            // Read word char by char
            while (!EndOfStream)
            {
                char c = reader.Char();

                // Word ends with a space or new-line, read it till the next word
                if (IsWhiteSpace(c) || IsNewline(c))
                {
                    // Apend newline(s)
                    if (IsNewline(c))
                    {
                        word.Append(c);
                    }

                    whiteSpaceFound = true;
                }
                else if (whiteSpaceFound)
                {
                    // C is a char from the next word
                    lookoutChar = c;
                    break;
                }
                else
                {
                    // Build the word char by char from non-whitespace characters
                    word.Append(c);
                }
            }

            // Return the composed word
            return word.ToString();
        }

        private bool IsWhiteSpace(char c)
        {
            return c == ' ' || c == '\t';
        }

        private bool IsNewline(char c)
        {
            return c == '\n';
        }

        private bool SkipWhiteSpace(char c)
        {
            return IsWhiteSpace(c) || IsNewline(c);
        }
    }
}
