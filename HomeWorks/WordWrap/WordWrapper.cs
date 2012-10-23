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
            
            if (args.Length != 3)
            {
                Console.WriteLine("Argument Error");
                return;
            }

            INPUT_FILE = args[0];
            OUTPUT_FILE = args[1];

            try
            {
                LINE_LENGTH = Convert.ToInt32(args[2]);
            }
            catch(Exception)
            {
                // Conversion errors
                Console.WriteLine("Argument Error");
                return;
            }

            if (LINE_LENGTH <= 0)
            {
                Console.WriteLine("Argument Error");
                return;
            }

// TODO: remove
            /*
for (int i = 1; i <= LINE_LENGTH; ++i)
    Console.Write(i % 10);
Console.WriteLine("");

LINE_LENGTH = 20;
             * */

            try
            {
                using (Reader reader = new Reader(INPUT_FILE))
                //using (StreamReader reader = new StreamReader(INPUT_FILE))
                using (StreamWriter writer = new StreamWriter(OUTPUT_FILE, false))
                {
                    LineProcessor lineProcessor = new LineProcessor(LINE_LENGTH, writer);
                    WordReader wordReader = new WordReader(reader);
                    //WordStreamedReader wordReader = new WordStreamedReader(reader);
                    
                    // Skip the first new line(s) if any
                    string word = wordReader.NextWord();
                    if (word != null && word.Replace("\n", "").Length > 0)
                        lineProcessor.AddWord(word);
                    
                    // Read words until the end of stream is reached
                    while (((word = wordReader.NextWord()) != null))
                    {
                        lineProcessor.AddWord(word);
                    }

                    lineProcessor.PrintLine();

                    writer.Flush();
                }
            }
            catch (Exception ex)
            {
                // Argument errors handled before, there are just file error left
                if (ex is System.UnauthorizedAccessException
                    || ex is System.ArgumentException
                    || ex is System.ArgumentNullException
                    || ex is System.NotSupportedException
                    || ex is System.Security.SecurityException
                    || ex is System.IO.IOException
                    || ex is System.IO.DirectoryNotFoundException
                    || ex is System.IO.PathTooLongException
                    || ex is System.IO.FileNotFoundException)
                {
                    Console.WriteLine("File Error");
                    return;
                }

                // Don't eat another exception
                throw ex;
            }
        }
    }

    /// <summary>
    /// Processes words to fit the line
    /// </summary>
    class LineProcessor
    {
        private readonly int MAXIMAL_LINE_LENGTH;
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
            this.MAXIMAL_LINE_LENGTH = maxLenght;
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
                bool endOfTheParagraph = word.Contains("\n");

                // Remove the newlines from the end of the word
                if (endOfTheParagraph)
                {
                    word = word.Replace("\n", "");
                }

                // New word doesn't fit the line -> print the line without it
                if (word.Length + CurrentLineLength + 1 > MAXIMAL_LINE_LENGTH)
                {
                    PrintLine();
                }

                // Add word to a line
                CurrentLineWords.Add(word);
                currentLineWordsLenght += word.Length;

                // Prevent from memory overflow when having 2 really big words in a row
                if (word.Length >= MAXIMAL_LINE_LENGTH)
                {
                    PrintLine();
                    endOfTheParagraph = false;
                }

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
        public void PrintLine(bool isEndOfParagraph = false)
        {
            // Nothing to print
            if (CurrentLineWords.Count == 0)
                return;

            /* Compute spaces between words which depends on:
                *  a) we are at the end of a paragraph
                *      - all two following words are separated with just one space
                *  b) the words neds to be separated equaly (from the left)
                *  c) there could be just 1 word on the line (too long for a line)
                */

            // Total spaces count left for a row
            int totalSpaceAvalilable = MAXIMAL_LINE_LENGTH - CurrentLineWords.Sum(word => word.Length);

            // Spaces count between each two words
            int spacesBetweenWords = 
                    (isEndOfParagraph)
                    ? 1 
                    : (CurrentLineWords.Count == 1) 
                        ? 0 
                        : totalSpaceAvalilable / (CurrentLineWords.Count - 1);

            // Spaces that left and will be used from left
            int spacesLeft = 
                    (isEndOfParagraph) 
                    ? 0 
                    : (CurrentLineWords.Count == 1) 
                        ? 0 
                        : totalSpaceAvalilable % (CurrentLineWords.Count - 1);
            
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
                if (--spacesLeft >= 0)
                {
                    line.Append(" ");
                }
            }

            string lineToPrint = line.ToString();

            // Write computed line
// TODO: Remove
//Console.Write(lineToPrint.Replace(" ", ":"));
//Console.WriteLine(string.Format("< {0} {1}", totalSpaceAvalilable, spacesBetweenWords));
            writer.WriteLine(lineToPrint);

            // Clear line after is printed
            CurrentLineWords.Clear();
            currentLineWordsLenght = 0;
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
            if (reader.EOF())
                return null;

            bool whiteSpaceFound = false;
            StringBuilder word = new StringBuilder();

            // Append char read in the previous method call
            if (lookoutChar.HasValue)
                word.Append(lookoutChar.Value);

            // Read stream char by char
            while (!reader.EOF())
            {
                char c = reader.Char();

                // Word ends with a space or new-line, read it till the next word
                if (IsWhiteSpace(c))
                {
                    // Apend newline(s) to process pararaphs
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

            // Output file has to end with a new line
            if (reader.EOF())
                word.Append("\n");

            // Return the composed word
            return word.ToString();
        }

        private bool IsWhiteSpace(char c)
        {
            return c == ' ' || c == '\t' || IsNewline(c);
        }

        private bool IsNewline(char c)
        {
            return c == '\n';
        }
    }

    /*

    class WordStreamedReader
    {
        StreamReader reader;
        int bufferSize = 16255;
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
     */
}
