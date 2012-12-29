using System;
using System.Collections.Generic;
using System.Text;
using System.IO;

namespace WordCounter2011 {

	// Readers are using a pull model.
	interface IWordReader {
		string ReadWord();
	}

	class WordReader : IWordReader, IDisposable {
		private TextReader reader;
		private StringBuilder buffer = new StringBuilder();

		public WordReader(TextReader reader) {
			this.reader = reader;
		}

		public string ReadWord() {
			int nextChar;

			while (true) {
				nextChar = reader.Read();
				if (nextChar == -1) {
					if (buffer.Length == 0) {
						return null;
					} else {
						break;
					}
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

	class Program {

		// The main word processing algorithm
		static void ProcessWords(IWordReader reader, IWordProcessor processor) {
			string word;
			while ((word = reader.ReadWord()) != null) {
				processor.ProcessWord(word);
			}

			processor.Finish();
		}

		static void Main(string[] args) {
			if (args.Length != 1 || args[0] == "") {
				Console.WriteLine("Argument Error");
				return;
			}

			WordReader reader = null;
			try {
				reader = new WordReader(new StreamReader(args[0]));
				var counter = new WordCounter();

				ProcessWords(reader, counter);
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
				if (reader != null) reader.Dispose();
			} 
		}
	}
}
