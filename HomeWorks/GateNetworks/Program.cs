using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace GateNetworks
{
    class Program
    {
        private static readonly int ITERATION_LIMIT = 1000000;
        private static readonly string COMMENTARY_START = ";";

        static void Main(string[] args)
        {
            if (args.Length != 1)
            {
                Console.WriteLine("Argument error.");
                return;
            }

            try
            {
                using (StreamReader reader = new StreamReader(args[0]))
                {
                    Network network = BuildNetwork(reader);

                    int iteration = 0;
                    while (iteration < ITERATION_LIMIT && network.HasChanged)
                    {   

                        ++iteration;
                    }
                }
            }
            catch (NetworkException ex)
            {
                Console.WriteLine(ex.Message);
            }
            catch (Exception)
            {
                Console.WriteLine("File error.");
            }
        }

        private static Network BuildNetwork(StreamReader reader)
        {
            int lineIndex = 1;
            while (!reader.EndOfStream)
            {
                string line = reader.ReadLine();
                if (!CanSkipLine(line))
                {
                    if (line.StartsWith("gate"))
                    {
                        Gate gate = ReadGate(reader, line.Substring(4), ref lineIndex);
                    }
                    else if (line.StartsWith("network"))
                    {

                    }
                    else
                    {
                        throw new SyntaxException(lineIndex);
                    }
                }
                
                ++lineIndex;
            }

            return null;
        }

        private static Gate ReadGate(StreamReader reader, string name, ref int lineIndex)
        {
            if (IsIdentifierValid(name))
            {
                var gate = new Gate(name);

                string line = String.Empty;
                do
                {
                    ++lineIndex;
                }
                while (!reader.EndOfStream && CanSkipLine(line = reader.ReadLine()))

                if (line.StartsWith("inputs"))
                {
                    string[] inputs = line.Split(new char[] { ' ' });
                    // Create InputPin for every identifier (inputs[0] is "inputs" keyword)
                    for (int i = 1; i < inputs.Length; ++i)
                    {
                        if (IsIdentifierValid(inputs[i]))
                        {
                            // Try register input -> failes when an input with the same identifier already exists
                            if (!gate.Inputs.RegisterPin(new InputPin(inputs[i])))
                            {
                                throw new DuplicateException(lineIndex);
                            }
                        }
                        else
                        {
                            throw new SyntaxException(lineIndex);
                        }
                    }
                }
                else
                {
                    throw new KeywordException(lineIndex);
                }

                line = String.Empty;
                do
                {
                    ++lineIndex;
                }
                while (!reader.EndOfStream && CanSkipLine(line = reader.ReadLine()))

                if (line.StartsWith("outputs"))
                {
                    string[] outputs = line.Split(new char[] { ' ' });
                    // Create OutputPin for every identifier (outputs[0] is "outputs" keyword)
                    for (int i = 1; i < outputs.Length; ++i)
                    {
                        if (IsIdentifierValid(outputs[i]))
                        {
                            // Try register output -> failes when an output with the same identifier already exists
                            if (!gate.Outputs.RegisterPin(new OutputPin(outputs[i])))
                            {
                                throw new DuplicateException(lineIndex);
                            }
                        }
                        else
                        {
                            throw new SyntaxException(lineIndex);
                        }
                    }
                }
                else
                {
                    throw new KeywordException(lineIndex);
                }

                line = String.Empty;
                do
                {
                    ++lineIndex;
                }
                while (!reader.EndOfStream && CanSkipLine(line = reader.ReadLine()))

                if (!line.StartsWith("end"))
                {
                    throw new SyntaxException(lineIndex);
                }

                return gate;
            }

            return null;
        }

        private static bool IsIdentifierValid(string identifier)
        {
            if (identifier.StartsWith("end"))
                return false;

            if (identifier.Contains("->"))
                return false;

            return identifier.Count(c => c == ' ' || c == '\t' || c == '\n' || c == '\r' || (int)c == 10 || c == '.' || c == ';') == 0;
        }

        private static bool CanSkipLine(string line)
        {
            if (String.IsNullOrWhiteSpace(line))
                return true;

            if (line.Length == 0)
                return true;

            if (line.StartsWith(COMMENTARY_START))
                return true;

            return false;
        }
    }

    class Gate
    {
        public PinsCollection Inputs { get; private set; }
        public PinsCollection Outputs { get; private set; }

        public string Name { get; private set; }

        public Gate(string name)
        {
            Inputs = new PinsCollection();
            Outputs = new PinsCollection();
            Name = name;
        }
    }

    class Network
    {
        public bool HasChanged { get; set; }
    }

    class PinsCollection
    {
        List<IPin> pins = new List<IPin>(10);

        public int Count { get; set; }

        public IPin this[string name]
        {
            get
            {
                return pins.FirstOrDefault(pin => pin.Name == name);
            }
        }

        public IPin this[int index]
        {
            get
            {
                if (index < 0 || index >= Count)
                    return null;

                return pins[index];
            }
        }

        public bool RegisterPin(IPin pin)
        {
            if (this[pin.Name] == null)
            {
                pins.Add(pin);

                ++Count;

                return true;
            }

            return false;
        }
    }

    public class InputPin : IPin
    {
        string Name { get; set; }

        bool? Value { get; set; }

        public InputPin(string name)
        {
            Name = name;
            Value = null;
        }
    }

    public class OutputPin : IPin
    {
        string Name { get; set; }

        bool? Value { get; set; }

        public OutputPin(string name)
        {
            Name = name;
            Value = null;
        }
    }

    interface IPin
    {
        string Name { get; set; }

        bool? Value { get; set; }
    }

    class NetworkException : Exception
    {
        public NetworkException(string message) : base(message) { }
    }

    class SyntaxException : NetworkException
    {
        public SyntaxException(int line) : base(String.Format("Line {0}: Syntax error.", line)) { } 
    }

    class DuplicateException : NetworkException
    {
        public DuplicateException(int line) : base(String.Format("Line {0}: Duplicate.", line)) { }
    }

    class KeywordException : NetworkException
    {
        public KeywordException(int line) : base(String.Format("Line {0}: Missing keyword.", line)) { }
    }
}
