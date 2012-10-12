using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace WordWrap
{
    class Program
    {
        private const int LINE_LENGTH = 60;
        private const int BUFFER_LENGTH = 100;
        private const string INPUT_FILE = "C:\\Temp\\screenshots.log";
        private const string OUTPUT_FILE = "C:\\Temp\\screenshots_ww.log";
        private const char NEW_LINE = '\n';
        private const char DELIMITER = ' ' ;

        static void Main(string[] args)
        {

            try
            {
                using (StreamReader reader = new StreamReader(INPUT_FILE))
                using (StreamWriter writer = new StreamWriter(OUTPUT_FILE, false))
                {
                    char[] buffer = new char[BUFFER_LENGTH];
                    CurrentLine currentLine = new CurrentLine();

                    while (!reader.EndOfStream)
                    {
                        string word = null;

                        // Read word untill it overflows the line or the end of stream is reached
                        while (currentLine.TotalLineLength < LINE_LENGTH && ((word = ReadWord(reader)) != null))
                        {
                            // Skip empty words (more spaces in a row)
                            if (word.Length > 0)
                            {
                                // Is this the end of a paragraph?
                                if (ContainsNewLine(word))
                                {
                                    // Word contains NEW_LINE(s)
                                    currentLine.PrintParagraph(writer, word);
                                }
                                else
                                {
                                    // This is just an ordinary word
                                    currentLine.AddWord(word);
                                }
                            }
                        } // while word is read

                        // Print line when is full or end of a file
                        currentLine.PrintLine(writer, word == null);

                    } // while !end of stream 

                    writer.Flush();
                } // using
            }
            catch (InvalidDataException ex)
            {
                Console.WriteLine();
                Console.WriteLine("--------");
                Console.WriteLine(ex.Message);
                Console.WriteLine();
            }
            finally
            {
                Console.Write("->");
                Console.ReadLine();
            }
        }

        public static bool ContainsNewLine(string word)
        {
            return word.Count(c => c == NEW_LINE) > 0;
        }

        private static string ReadWord(StreamReader reader)
        {
            if (reader.EndOfStream)
                return null;

            char[] buffer = new char[1];
            StringBuilder word = new StringBuilder();

            // REad word char by char
            while (reader.Read(buffer, 0, 1) == 1)
            {
                // Word ends with a space
                if (buffer[0] == ' ')
                    break;

                // Build the word
                word.Append(buffer[0]);
            }

            return word.ToString();
        }

        private class CurrentLine
        {
            /// <summary>
            /// Total count of alphabet characters in current line
            /// </summary>
            public int CharacterCount { get; private set; }

            /// <summary>
            /// Total count of alphabet characters in current line + 1 space for each two following words 
            /// </summary>
            public int TotalLineLength 
            {
                get
                {
                    return CharacterCount + Math.Max(0, Words.Count - 1);
                }
            }

            /// <summary>
            /// The list of words in the current line
            /// </summary>
            public LinkedList<string> Words { get; private set; }

            /// <summary>
            /// Creates an empty instance of CurrentLine
            /// </summary>
            public static CurrentLine Empty
            {
                get { return new CurrentLine(); }
            }

            /// <summary>
            /// Default .ctor
            /// </summary>
            public CurrentLine()
            {
                this.Words = new LinkedList<string>();
            }

            /// <summary>
            /// Adds word to the current line
            /// </summary>
            /// <param name="word">Word to add</param>
            public void AddWord(string word)
            {
                Words.AddLast(word);
                CharacterCount += word.Length;
            }

            private void Clear()
            {
                Words.Clear();
                CharacterCount = 0;
            }

            public void PrintLine(StreamWriter writer, bool endOfTheParagraph)
            {
                // The word that would exceed the line
                string trailingWord = String.Empty; 

                // Extract the trailing word
                if (TotalLineLength > LINE_LENGTH)
                {
                    // Get the trailing word
                    trailingWord = RemoveLastWord();

                    // Check the word size - just for sure
                    if (trailingWord.Length > LINE_LENGTH)
                        throw new InvalidDataException(
                            String.Format("The word '{0}' is too long", trailingWord));
                }

                // Print the current line
                // We are on the end of paragraph if the last word in the paragraph is in the current line
                PrintLineInternal(writer, trailingWord.Length == 0 && endOfTheParagraph);

                if (trailingWord.Length > 0)
                {
                    AddWord(trailingWord);

                    // Print the only one word
                    if (endOfTheParagraph)
                    {
                        PrintLineInternal(writer, true);
                    }
                }
            }

            /// <summary>
            /// Prints paragraph(s) proeprly
            /// </summary>
            /// <param name="writer">Writer to output</param>
            /// <param name="lastWord">Last word with NEW_LINE(s)</param>
            public void PrintParagraph(StreamWriter writer, string lastWord)
            {
                string[] paragraphParts = lastWord.Split(NEW_LINE);

                var endOfParagraph = paragraphParts[0];

                // Solve the end of current paragraph
                if (endOfParagraph.Length == 0)
                {
                    // This case when the whole text begins with some NEW_LINEs
                    // Prints a simple new line
                    Empty.PrintLine(writer, true);
                }
                else
                {
                    // This is the regular end of the paragraph
                    AddWord(endOfParagraph);
                    PrintLine(writer, true);
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
                        CurrentLine.Empty.PrintLine(writer, true);
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

                var lastWord = Words.ElementAt(Words.Count - 1);

                // Remove word and subtract its size
                Words.RemoveLast();
                CharacterCount -= lastWord.Length;

                return lastWord;
            }

            /// <summary>
            /// Prints line
            /// </summary>
            /// <param name="writer">Output writer</param>
            /// <param name="isEndOfParagraph">Is the end of the paragraph?</param>
            private void PrintLineInternal(StreamWriter writer, bool isEndOfParagraph)
            {
                // Compute spaces betwwen words
                int wordIndex = 1;
                int totalSpaceAvalilable = LINE_LENGTH - CharacterCount;
                int spacesBetweenWords = isEndOfParagraph ? 1 : totalSpaceAvalilable / (Words.Count - 1);
                int spacesLeft = isEndOfParagraph ? 0 : totalSpaceAvalilable % (Words.Count - 1);
            
                StringBuilder line = new StringBuilder();

                // Print words with proper spacing
                foreach (var word in Words)
                {
                    line.Append(word);

                    // Spaces between each 2 following words
                    int spacesUsed = 0;
                    while (++spacesUsed <= spacesBetweenWords)
                    {
                        line.Append(" ");
                    }

                    // Additional spaces from left
                    if (wordIndex <= spacesLeft)
                    {
                        line.Append(" ");
                    }

                    ++wordIndex;
                }

                // Write computed line
                Console.WriteLine(line.ToString());
                writer.WriteLine(line.ToString());

                // Clear line after is printed
                Clear();
            }
        }
    }
}
