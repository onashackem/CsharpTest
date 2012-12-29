using System;
using System.Collections.Generic;
using System.Text;
using System.IO;

namespace WordWrap2011 {

	// Readers are using a pull model.
	interface IWordReader {
		string ReadWord();
	}

	static class MarkerWords {
		public const string NewLine = "\n";
	}

	class WordReader : IWordReader, IDisposable {
		private TextReader reader;
		private bool detectNewLines;

		private StringBuilder buffer = new StringBuilder();
		private bool delayedNewLine = false;

		public WordReader(TextReader reader)
			: this(reader, false) {
		}

		public WordReader(TextReader reader, bool detectNewLines) {
			this.reader = reader;
			this.detectNewLines = detectNewLines;
		}

		public string ReadWord() {
			if (delayedNewLine) {		// detectNewLines == false implies delayedNewLine == false all the time!
				delayedNewLine = false;
				return MarkerWords.NewLine;
			}

			int nextChar;
			while (true) {
				nextChar = reader.Read();
				if (nextChar == -1) {
					if (buffer.Length == 0) {
						return null;
					} else {
						break;
					}

					#region New-line handling: the whole "if" statement in this region can be omited if new-lines are considered just as plain whitespaces.
				} else if (detectNewLines && nextChar == '\n') {
					if (buffer.Length == 0) {
						return MarkerWords.NewLine;
					} else {
						delayedNewLine = true;
						break;
					}
					#endregion

				} else if (char.IsWhiteSpace((char) nextChar)) {
					if (buffer.Length != 0) {
						break;
					} // else Skip leading whitespace
				} else {
					buffer.Append((char) nextChar);
				}
			}

			string word = buffer.ToString(0, buffer.Length);
			buffer.Length = 0;
			return word;
		}

		public void Dispose() {
			reader.Dispose();
		}
	}

	// Processors are using a push model.
	interface IWordProcessor {
		void ProcessWord(string word);
		void Finish();
	}

	// A class from the previous assignment.
	// It it not used anywhere in this assignment, but is left here just to demonstrate it did not change at all.
	class WordCounter : IWordProcessor {
		private Dictionary<string, int> wordCounts = new Dictionary<string, int>();

		public void ProcessWord(string word) {
			int count;
			wordCounts.TryGetValue(word, out count);
			wordCounts[word] = count + 1;
		}

		public void Finish() {
			foreach (KeyValuePair<string, int> pair in wordCounts) {
				Console.WriteLine(pair.Key + ": " + pair.Value.ToString());
			}
		}
	}

	class WordWrapProcessor : IWordProcessor, IDisposable {
		private char[] arrayOfSpaces;

		private TextWriter writer;
		private int targetWidth;

		private List<string> currentLine = new List<string>();	// List of all words on the current line.
		private int currentLineWidth = 0;						// Sum of lengths of all words currently in the currentLine list.

		private bool newLineDetected = false;
		private bool firstParagraphStarted = false;
		private bool closePreviousParagraph = false;

		public WordWrapProcessor(TextWriter writer, int targetWidth) {
			this.writer = writer;
			this.targetWidth = targetWidth;

			arrayOfSpaces = new char[targetWidth];
			for (int i = 0; i < arrayOfSpaces.Length; i++) {
				arrayOfSpaces[i] = ' ';
			}
		}

		public void ProcessWord(string word) {
			if (word == MarkerWords.NewLine) {
				if (newLineDetected) {				// Two or more consecutive new-lines => end of a paragraph.
					closePreviousParagraph = true;
				} else {							// A new-line following a plain word => possible end of a paragraph,
													// needs to be verified when next word arrives.
					newLineDetected = true;			
				}
			} else {
				firstParagraphStarted = true;
				newLineDetected = false;

				if (closePreviousParagraph) {
					if (currentLineWidth > 0) {
						WriteLineLeftJustify(currentLine);
						currentLine.Clear();
						currentLineWidth = 0;
					}
					writer.WriteLine();
					closePreviousParagraph = false;
				}

				if ((word.Length > targetWidth || currentLineWidth + currentLine.Count + word.Length > targetWidth) && currentLineWidth > 0) {
					// The word does not fit into the current line, so flush it to the output.
					WriteLineBlockJustify(currentLine, currentLineWidth);
					currentLine.Clear();
					currentLineWidth = 0;
				}

				if (word.Length > targetWidth) {	// Special case for too long words.
					writer.WriteLine(word);
				} else {	// Add the word to the current line buffer.
					currentLine.Add(word);
					currentLineWidth += word.Length;
				}
			}
		}

		public void Finish() {
			if (currentLine.Count > 0) {
				WriteLineLeftJustify(currentLine);
			} else if (!firstParagraphStarted) {	// Special case for an input file containing only white spaces.
				writer.WriteLine();
			}
		}

		public void Dispose() {
			writer.Dispose();
		}

		private void WriteLineLeftJustify(List<string> line) {
			for (int i = 0; i < line.Count; i++) {
				if (i != 0) {
					writer.Write(' ');
				}
				writer.Write(line[i]);
			}
			writer.WriteLine();
		}

		private void WriteLineBlockJustify(List<string> line, int totalWordsWidth) {
			if (line.Count == 0) {
				writer.WriteLine();
			} else if (line.Count == 1) {
				writer.WriteLine(line[0]);
			} else {
				int spacesPerLine = targetWidth - totalWordsWidth - (line.Count - 1);
				int spacesPerWord = 1 + spacesPerLine / (line.Count - 1);
				int surplusSpaces = spacesPerLine % (line.Count - 1);

				for (int i = 0; i < line.Count; i++) {
					if (i != 0) {
						writer.Write(arrayOfSpaces, 0, spacesPerWord + (surplusSpaces-- > 0 ? 1 : 0));
					}
					writer.Write(line[i]);
				}
				writer.WriteLine();
			}
		}
	}

	class Program {

		// The main word processing algorithm - exactly the same as in the WordCounter assignment.
		static void ProcessWords(IWordReader reader, IWordProcessor processor) {
			string word;
			while ((word = reader.ReadWord()) != null) {
				processor.ProcessWord(word);
			}

			processor.Finish();
		}

		static void Main(string[] args) {
			if (args.Length != 3 || args[0] == "" || args[1] == "") {
				Console.WriteLine("Argument Error");
				return;
			}

			int width;
			if (!int.TryParse(args[2], out width) || width <= 0) {
				Console.WriteLine("Argument Error");
				return;
			}

			WordReader reader = null;
			WordWrapProcessor processor = null;
			try {
				reader = new WordReader(new StreamReader(args[0]), true);
				processor = new WordWrapProcessor(new StreamWriter(args[1]), width);

				ProcessWords(reader, processor);
			} catch (FileNotFoundException) {
				Console.WriteLine("File Error");
			} catch (IOException) {
				Console.WriteLine("File Error");
			} catch (UnauthorizedAccessException) {
				Console.WriteLine("File Error");
			} catch (System.Security.SecurityException) {
				Console.WriteLine("File Error");
			} catch (ArgumentException) {
				Console.WriteLine("File Error");
			} finally {
				if (processor != null) processor.Dispose();
				if (reader != null) reader.Dispose();
			}
		}
	}
}
