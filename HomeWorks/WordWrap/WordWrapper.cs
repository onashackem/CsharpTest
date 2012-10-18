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

                using (Reader reader = new Reader(INPUT_FILE))
                using (StreamWriter writer = new StreamWriter(OUTPUT_FILE, false))
                {
                    CurrentLine currentLine = new CurrentLine(LINE_LENGTH);

                    while (!reader.EOF())
                    {
                        string word = null;

                        // Read word untill it overflows the line or the end of stream is reached
                        while (currentLine.MinimalLineLength < LINE_LENGTH && ((word = ReadWord(reader)) != null))
                        {
                            // Skip empty words (more spaces in a row)
                            if (word.Length > 0)
                            {
                                // Is this the end of a paragraph?
                                if (ContainsNewLine(word))
                                {
                                    // Word contains NEW_LINE(s)
                                    currentLine.PrintParagraphs(writer, word, reader.EOF());
                                }
                                else
                                {
                                    // This is just an ordinary word
                                    currentLine.AddWord(word);
                                }
                            }
                        } // while word is read

                        // Print line when is full or end of a file
                        if (currentLine.Words.Count > 0)
                            currentLine.PrintLine(writer, word == null);

                    } // while !end of stream 

                    writer.Flush();
                } // using
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

        public static bool ContainsNewLine(string word)
        {
            return word.Count(c => c == '\n') > 0;
        }

        private static string ReadWord(Reader reader)
        {
            if (reader.EOF())
                return null;

            char c;
            StringBuilder word = new StringBuilder();

            // REad word char by char
            while (!reader.EOF())
            {
                c = reader.Char();

                // Word ends with a space
                if (c == ' ' || c == '\t')
                    break;

                // Build the word
                word.Append(c);
            }

            return word.ToString();
    
        }
    }

    class CurrentLine
    {
        private readonly int maximalLineLength;

        /// <summary>
        /// Sum of lenthg of all words in the current line + 1 space between each two following words 
        /// </summary>
        public int MinimalLineLength 
        {
            get
            {
                return Words.Sum(word => word.Length) + Math.Max(0, Words.Count - 1);
            }
        }

        /// <summary>
        /// The list of words in the current line
        /// </summary>
        public LinkedList<string> Words { get; private set; }

        /// <summary>
        /// Default .ctor
        /// </summary>
        public CurrentLine(int maxLenght)
        {
            maximalLineLength = maxLenght;
            this.Words = new LinkedList<string>();
        }

        /// <summary>
        /// Adds word to the current line
        /// </summary>
        /// <param name="word">Word to add</param>
        public void AddWord(string word)
        {
            Words.AddLast(word);
        }

        public void PrintLine(StreamWriter writer, bool endOfTheParagraph)
        {
            // The word that would exceed the line
            string trailingWord = String.Empty; 

            // Extract the trailing word
            if (MinimalLineLength > this.maximalLineLength)
            {
                // Get the trailing word
                trailingWord = RemoveLastWord();
            }

            // Print the current line
            // We are on the end of paragraph if the last word in the paragraph is in the current line
            PrintLineInternal(writer, trailingWord.Length == 0 && endOfTheParagraph);

            if (trailingWord.Length > 0)
            {
                AddWord(trailingWord);
                                        
                // Print the only one word if it is the only word on line because
                //  a) is end of a pragraph or
                //  b) is greater itself than line length
                if (endOfTheParagraph || trailingWord.Length > this.maximalLineLength)
                {
                    PrintLineInternal(writer, endOfTheParagraph);
                }
            }
        }

        /// <summary>
        /// Prints paragraph(s) proeprly
        /// </summary>
        /// <param name="writer">Writer to output</param>
        /// <param name="lastWord">Last word with NEW_LINE(s)</param>
        public void PrintParagraphs(StreamWriter writer, string lastWord, bool endOfFile)
        {
            string[] paragraphParts = lastWord.Split('\n');

            var endOfParagraph = paragraphParts[0];

            // Solve the end of current paragraph
            if (endOfParagraph.Length == 0)
            {
                // This case when the whole text begins with some NEW_LINEs
                // Prints a simple new line
                new CurrentLine(maximalLineLength).PrintLine(writer, true);
            }
            else
            {
                // This is the regular end of the paragraph
                AddWord(endOfParagraph);
                PrintLine(writer, true);
            }

            // Skip all new lines before the end of file
            if (endOfFile)
            {
                paragraphParts = paragraphParts.Reverse().SkipWhile(word => word == string.Empty).Reverse().ToArray();
            }

            // Current line is empty
            // Now we have to distinct 3 possible options
            //  1. Just a new paragraph begins (word was xxx.\nAyy)
            //  2. There could be multiple newlines in a row (word was xxx.\n\n\nYyyy)
            //  3. There are multiple one-word pararaphs (word was xxx.\n\nYyy.\nZzz.\nAaa)
            for (int index = 1; index < paragraphParts.Length; ++index)
            {
                if (paragraphParts[index].Length == 0)
                {
                    // Just a new line (case 2)
                    new CurrentLine(maximalLineLength).PrintLine(writer, true);
                }
                else
                {
                    // Start a new paragraph (cases 1 and 3)
                    AddWord(paragraphParts[index]);

                    // This is a case 3 - one word paragraph
                    if (index < paragraphParts.Length - 1)
                        PrintLine(writer, true);
                }
            }
        }

        /// <summary>
        /// Removes last word from the curret line
        /// </summary>
        /// <returns>Returns the removed word</returns>
        private string RemoveLastWord()
        {
            if (Words.Count == 0)
                return String.Empty;

            var lastWord = Words.Last.Value;

            // Remove word and subtract its size
            Words.RemoveLast();

            return lastWord;
        }

        /// <summary>
        /// Prints line
        /// </summary>
        /// <param name="writer">Output writer</param>
        /// <param name="isEndOfParagraph">Is the end of the paragraph?</param>
        private void PrintLineInternal(StreamWriter writer, bool isEndOfParagraph)
        {
            /* Compute spaces between words which depends on:
                *  a) we are at the end of a paragraph
                *      - all two following words are separated with just one space
                *  b) the words neds to be separated equaly (from the left)
                *  c) there could be just 1 word on the line (too long for a line)
                */

            int totalSpaceAvalilable = this.maximalLineLength - Words.Sum(word => word.Length);
            int spacesBetweenWords = isEndOfParagraph ? 1 : (Words.Count <= 1 ? 0 : totalSpaceAvalilable / (Words.Count - 1));
            int spacesLeft = isEndOfParagraph ? 0 : (Words.Count <= 1 ? 0 : totalSpaceAvalilable % (Words.Count - 1));
            
            StringBuilder line = new StringBuilder();

            // Print words with proper spacing
            foreach (var word in Words)
            {
                line.Append(word);

                // No spaces after the last word
                if (word == Words.Last.Value)
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
            if (!isEndOfParagraph && Words.Count > 1)
                System.Diagnostics.Debug.Assert(lineToPrint.Length == this.maximalLineLength);

            // Write computed line
            Console.WriteLine(lineToPrint);
            writer.WriteLine(lineToPrint);

            // Clear line after is printed
            Words.Clear();
        }
    }
}
