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
                    network.DoFirstInitialization();

                    string input;
                    while ((input = Console.ReadLine()) != null && !input.StartsWith("end"))
                    {
                        try
                        {
                            string[] splittedInput = input.SplitBySpaces();
                            if (splittedInput.Length != network.Inputs.Count - 2)
                                throw new SyntaxException();
                                                        
                            network.DoRegularInitialization();

                            var inputValues = splittedInput.Select(b => ParseBool(b)).ToArray();
                            network.SetInputs(inputValues);

                            int iteration = 0;
                            while (iteration < ITERATION_LIMIT && network.HasChanged)
                            {
                                ++iteration;
                                
                                /*
                                Program.WriteLine("-----");
                                Program.WriteLine();
                                Program.WriteLine(String.Format("Tick #{0}", iteration));
                                 */
                                network.Tick();
                            }

                            string pinValues = "";
                            network.Outputs.ExecuteActionForEachPin(pin => pinValues += String.Format("{0} ", pin.Value.HasValue ? (pin.Value.Value ? "1" : "0") : "?"));
                            Console.WriteLine(String.Format("{0} {1}", iteration, pinValues.Remove(pinValues.Length - 1)));                            
                        }
                        catch (NetworkException ex)
                        {
                            Console.WriteLine(ex.Message);
                        }
                    }
                }
            }
            catch (NetworkException ex)
            {
                Console.WriteLine(ex.Message);
            }
            catch (Exception ex)
            {
                Console.WriteLine("File error.");
            }
        }

        public static void Write(string text) 
        { 
            //Console.Write(text); 
        }
        
        public static void WriteLine(string text = "") 
        {
            //Console.WriteLine(text); 
        }

        private static Network BuildNetwork(StreamReader reader)
        {
            Dictionary<string, Gate> gates = new Dictionary<string, Gate>();
            int lineIndex = 1;
            while (!reader.EndOfStream)
            {
                string line = reader.ReadLine();
                if (!CanSkipLine(line))
                {
                    if (line.StartsWith("gate"))
                    {
                        Gate gate = ReadGate(reader, line.Substring(5), ref lineIndex, gates);

                        gates.Add(gate.Name, gate);
                    }
                    else if (line.StartsWith("network"))
                    {
                        return ReadNetwork(reader, ref lineIndex, gates);
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

        private static Network ReadNetwork(StreamReader reader, ref int lineIndex, Dictionary<string, Gate> gates)
        {
            var network = new Network();

            string line = String.Empty;
            do
            {
                ++lineIndex;
            }
            while (!reader.EndOfStream && CanSkipLine(line = reader.ReadLine()));

            if (line.StartsWith("inputs"))
            {
                string[] inputs = line.SplitBySpaces();
                // Create InputPin for every identifier (inputs[0] is "inputs" keyword)
                for (int i = 1; i < inputs.Length; ++i)
                {
                    if (IsIdentifierValid(inputs[i]))
                    {
                        // Try register input -> failes when an input with the same identifier already exists
                        if (!network.Inputs.RegisterPin(new Pin(inputs[i])))
                        {
                            throw new DuplicateException(lineIndex);
                        }
                    }
                    else
                    {
                        throw new SyntaxException(lineIndex);
                    }
                }

                // Two implicit inputs belongs to the end of Inputs collection
                network.Inputs.AddPin(new Pin("0"));
                network.Inputs.AddPin(new Pin("1"));
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
            while (!reader.EndOfStream && CanSkipLine(line = reader.ReadLine()));

            if (line.StartsWith("outputs"))
            {
                string[] outputs = line.SplitBySpaces();
                // Create OutputPin for every identifier (outputs[0] is "outputs" keyword)
                for (int i = 1; i < outputs.Length; ++i)
                {
                    if (IsIdentifierValid(outputs[i]))
                    {
                        // Try register output -> failes when an output with the same identifier already exists
                        if (!network.Outputs.RegisterPin(new Pin(outputs[i])))
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

            List<string> connectedOutputs = new List<string>(network.Outputs.Count);
            while (!reader.EndOfStream)
            {
                line = reader.ReadLine();
                ++lineIndex;

                if (CanSkipLine(line))
                {
                    continue;
                }
                else if (line.StartsWith("end"))
                {
                    if (!ValidateNetwork(network))
                        throw new BindingException(lineIndex);

                    if (connectedOutputs.Count != network.Outputs.Count)
                        throw new BindingException(lineIndex);

                    return network;
                }
                else if (line.StartsWith("gate"))
                {
                    string[] ids = line.SplitBySpaces();

                    if (ids.Length != 3)
                        throw new SyntaxException(lineIndex);

                    string gateInstanceName = ids[1];
                    string gateType = ids[2];

                    if (!gates.ContainsKey(gateType))
                        throw new SyntaxException(lineIndex);

                    if (!network.RegisterGateInstance(gates[gateType].CreateInstance(gateInstanceName, network)))
                        throw new DuplicateException(lineIndex);
                }
                else
                {
                    if (network.GateInstances.Count == 0)
                        throw new KeywordException(lineIndex);

                    if (!line.Contains("->"))
                        throw new SyntaxException(lineIndex);

                    string[] definition = line.Split(new string[] { "->" }, StringSplitOptions.RemoveEmptyEntries);
                    if (definition.Length != 2)
                        throw new SyntaxException(lineIndex);

                    // Find pins to register redirect
                    bool isFromInput;
                    bool isToInput;
                    IPin from = FindPin(definition[1], network, lineIndex, out isFromInput);
                    IPin to = FindPin(definition[0], network, lineIndex, out isToInput);

                    if (isFromInput && isToInput)
                        throw new BindingException(lineIndex);

                    if (!from.RegisterPinForRedirect(to))
                        throw new DuplicateException(lineIndex);

                    if (network.Outputs[to.Name] != null)
                    {
                        if (!connectedOutputs.Contains(to.Name))
                            connectedOutputs.Add(to.Name);
                    }
                }
            }

            throw new SyntaxException(lineIndex);
        }

        private static bool ValidateNetwork(Network network)
        {
            bool isValid = true;
            network.Inputs.ExecuteActionForEachPin(pin => 
            { 
                if ((pin.Name != "0" && pin.Name != "1") && (pin.RedirectTo == null || pin.RedirectTo.Count == 0)) 
                    isValid = false; 
            });

            return isValid;
        }

        private static IPin FindPin(string identifier, Network network, int lineIndex, out bool isInputPin)
        {
            isInputPin = false;

            if (identifier.Contains('.'))
            {
                string[] definition = identifier.Split(new char[] { '.' });
                if (definition.Length != 2)
                    throw new SyntaxException(lineIndex);

                if (network.GateInstances.ContainsKey(definition[0]))
                {
                    if (network.GateInstances[definition[0]].Inputs[definition[1]] != null)
                    {
                        return network.GateInstances[definition[0]].Inputs[definition[1]];
                    }

                    if (network.GateInstances[definition[0]].Outputs[definition[1]] != null)
                    {
                        return network.GateInstances[definition[0]].Outputs[definition[1]];
                    }
                }
                
                throw new SyntaxException(lineIndex);
            }
            else if (network.Inputs[identifier] != null)
            {
                isInputPin = true;
                return network.Inputs[identifier];
            }
            else if (network.Outputs[identifier] != null)
            {
                return network.Outputs[identifier];
            }
            else
                throw new SyntaxException(lineIndex);

        }

        private static Gate ReadGate(StreamReader reader, string name, ref int lineIndex, Dictionary<string, Gate> gates)
        {
            if (IsIdentifierValid(name))
            {
                if (gates.ContainsKey(name))
                    throw new DuplicateException(lineIndex);

                var gate = new Gate(name);

                string line = String.Empty;
                do
                {
                    ++lineIndex;
                }
                while (!reader.EndOfStream && CanSkipLine(line = reader.ReadLine()));

                if (line.StartsWith("inputs"))
                {
                    string[] inputs = line.SplitBySpaces();
                    // Create InputPin for every identifier (inputs[0] is "inputs" keyword)
                    for (int i = 1; i < inputs.Length; ++i)
                    {
                        if (IsIdentifierValid(inputs[i]))
                        {
                            // Try register input -> failes when an input with the same identifier already exists
                            if (!gate.RegisterInputIdentifier(inputs[i]))
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
                while (!reader.EndOfStream && CanSkipLine(line = reader.ReadLine()));

                if (line.StartsWith("outputs"))
                {
                    string[] outputs = line.SplitBySpaces();
                    // Create OutputPin for every identifier (outputs[0] is "outputs" keyword)
                    for (int i = 1; i < outputs.Length; ++i)
                    {
                        if (IsIdentifierValid(outputs[i]))
                        {
                            // Try register output -> failes when an output with the same identifier already exists
                            if (!gate.RegisterOutputIdentifier(outputs[i]))
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
                
                while (!reader.EndOfStream)
                {
                    line = reader.ReadLine();
                    ++lineIndex;

                    if (CanSkipLine(line))
                    {
                        continue;
                    }
                    else if (line.StartsWith("end"))
                    {
                        return gate;
                    }
                    else if (line.StartsWith("0") || line.StartsWith("1") || line.StartsWith("?"))
                    {
                        string[] def = line.SplitBySpaces();

                        if (def.Length != gate.InputIdentifiers.Count + gate.OutputIdentifiers.Count)
                            throw new SyntaxException(lineIndex);

                        bool?[] inputs = new bool?[gate.InputIdentifiers.Count];
                        bool?[] outputs = new bool?[gate.OutputIdentifiers.Count];

                        for (int i = 0; i < inputs.Length; ++i)
                        {
                            inputs[i] = ParseBool(def[i], lineIndex);
                        }

                        for (int i = 0; i < outputs.Length; ++i)
                        {
                            outputs[i] = ParseBool(def[inputs.Length + i], lineIndex);
                        }

                        if (!gate.RegisterTransitionFunction(new TransitionFunction(inputs, outputs)))
                            throw new DuplicateException(lineIndex);
                    }
                    else
                    {
                        // Will throw a syntax error
                        break;
                    }
                }
                
                throw new SyntaxException(lineIndex);
            }

            return null;
        }

        private static bool? ParseBool(string value, int? lineIndex = null)
        {
            if (value == "0")
                return false;
            else if (value == "1")
                return true;
            else if (value == "?")
                return null;
            else
            {
                if (lineIndex.HasValue)
                    throw new SyntaxException(lineIndex.Value);
                else
                    throw new SyntaxException();
            }
        }

        private static bool IsIdentifierValid(string identifier)
        {
            if (identifier.StartsWith("end"))
                return false;

            if (identifier.Contains("->"))
                return false;

            return identifier.Count(c => c.ToString().Trim().Length == 0 || c == '.' || c == ';') == 0;
        }

        private static bool CanSkipLine(string line)
        {
            if (line == null)
                return true;

            if (line.Trim().Length == 0)
                return true;

            if (line.StartsWith(COMMENTARY_START))
                return true;

            return false;
        }
    }

    class Gate
    {
        public List<string> InputIdentifiers  { get; private set; }
        public List<string> OutputIdentifiers  { get; private set; }
        public List<TransitionFunction> Functions  { get; private set; }

        private TransitionFunction _notMatchedFunction;
        private TransitionFunction NotMatchedFunction
        {
            get
            {
                if (_notMatchedFunction == null)
                {
                    _notMatchedFunction = new TransitionFunction(
                        new bool?[InputIdentifiers.Count],
                        new bool[OutputIdentifiers.Count].Select(b => (bool?)b).ToArray()
                    );
                }

                return _notMatchedFunction;
            }
        }

        private TransitionFunction _undefinedInputFunction;
        private TransitionFunction UndefinedInputFunction
        {
            get
            {
                if (_undefinedInputFunction == null)
                {
                    _undefinedInputFunction = new TransitionFunction(
                        new bool?[InputIdentifiers.Count],
                        new bool?[OutputIdentifiers.Count]
                    );
                }

                return _undefinedInputFunction;
            }
        }

        public string Name { get; private set; }

        public Gate(string name)
        {
            InputIdentifiers = new List<string>(10);
            OutputIdentifiers = new List<string>(10);
            Functions = new List<TransitionFunction>(20);
            Name = name;
        }

        public bool RegisterTransitionFunction(TransitionFunction function)
        {
            if (Functions.Find(f => f.Matches(function)) != null)
                return false;

            Functions.Add(function);
            return true;
        }

        public TransitionFunction FindTransition(bool?[] inputs)
        {
            var function = Functions.FirstOrDefault(f => f.Matches(inputs));

            if (function != null)
                return function;

            foreach (var b in inputs)
            {
                if (!b.HasValue)
                    return UndefinedInputFunction;
            }

            return NotMatchedFunction;
        }

        public GateInstance CreateInstance(string name, Network network)
        {
            PinsCollection inputPins = new PinsCollection();
            PinsCollection outputPins = new PinsCollection();
            
            InputIdentifiers.ForEach(id => inputPins.AddPin(new Pin(id)));
            OutputIdentifiers.ForEach(id => outputPins.AddPin(new Pin(id)));

            return new GateInstance(name, inputPins, outputPins, this, network);
        }

        public bool RegisterOutputIdentifier(string identifier)
        {
            return RegisterPinIdentifier(identifier, OutputIdentifiers);
        }

        public bool RegisterInputIdentifier(string identifier)
        {
            return RegisterPinIdentifier(identifier, InputIdentifiers);
        }

        private bool RegisterPinIdentifier(string identifier, List<string> pinsCollection)
        {
            if (pinsCollection.Find(i => i == identifier) != null)
                return false;

            pinsCollection.Add(identifier);
            return true;
        }
    }

    class GateInstance
    {
        public PinsCollection Inputs { get; private set; }
        public PinsCollection Outputs { get; private set; }

        public string Name { get; private set; }
        public bool IsModified { get; set; }

        private Network network;
        private Gate originGate;

        public GateInstance(string name, PinsCollection inputs, PinsCollection outputs, Gate originGate, Network network)
        {
            Inputs = inputs;
            Outputs = outputs;
            Name = name;

            this.network = network;
            this.originGate = originGate;
        }

        public void Initialize()
        {
            Inputs.ExecuteActionForEachPin(pin => 
                {
                    pin.OnValueChangedAction = () => IsModified = true;
                    /*
                    if (pin.RedirectTo != null)
                        pin.RedirectTo.ExecuteActionForEachPin(p => Program.WriteLine(String.Format("\t{0} -> {1}", pin.Name, p.Name)));
                     */
                }
            );

            if (Inputs.Count == 0)
            {
                Compute();
                Outputs.ExecuteActionForEachPin(pin => pin.Redirect() );
            }
        }

        public void Tick()
        {
            IsModified = false;

            Compute();
        }


        private void Compute()
        {
            var inputs = Inputs.GetValues();
            var transition = originGate.FindTransition(inputs);

            //Program.Write("\tFor inputs: ");
            PrintValues(inputs);
            //Program.Write("\tFound outputs: ");
            PrintValues(transition.Outputs);

            for (int i = 0; i < transition.Outputs.Length; ++i)
            {
                Outputs[i].Value = transition.Outputs[i];
            }
        }

        private void PrintValues(bool?[] values)
        {
            for (int i = 0; i < values.Length; ++i)
            {
                Program.Write(String.Format("'{0}' ", values[i]));
            }

            Program.WriteLine();
        }
    }

    class Network
    {
        private List<GateInstance> GateInstancesWithModifiedInput 
        {
            get
            {
                return GateInstances.Values.Where(gi => gi.IsModified).ToList();
            }
        }

        public bool HasChanged 
        { 
            get 
            {
                return GateInstances.Values.FirstOrDefault(gi => gi.IsModified) != null ; 
            } 
        }

        public PinsCollection Inputs { get; private set; }
        public PinsCollection Outputs { get; private set; }
        public Dictionary<string, GateInstance> GateInstances { get; private set; }

        public Network()
        {
            Inputs = new PinsCollection();
            Outputs = new PinsCollection();
            GateInstances = new Dictionary<string, GateInstance>();
        }

        public bool RegisterGateInstance(GateInstance gateInstance)
        {
            if (GateInstances.ContainsKey(gateInstance.Name))
                return false;

            GateInstances.Add(gateInstance.Name, gateInstance);
            return true;
        }

        public void Tick()
        {
            var gatesToCompute = GateInstancesWithModifiedInput;
            //Program.WriteLine(String.Format("Modified gates: {0}.", gatesToCompute.Count()));

            foreach (var gi in gatesToCompute)
            {
                /*
                Program.WriteLine(String.Format("Computing '{0}' gate instance: ", gi.Name));
                Program.Write(String.Format("Gate {0}.Inputs: ", gi.Name));
                gi.Inputs.ExecuteActionForEachPin(pin => Program.Write(String.Format("{0}: '{1}', ", pin.Name, pin.Value)));
                Program.WriteLine();
                */
                gi.Tick();
                /*
                Program.Write(String.Format("Gate {0}.Outputs: ", gi.Name));
                gi.Outputs.ExecuteActionForEachPin(pin => Program.Write(String.Format("{0}: '{1}', ", pin.Name, pin.Value)));
                Program.WriteLine();
                Program.WriteLine(String.Format("Finished computing '{0}' gate instance: ", gi.Name));
                */
            }

            foreach (var gi in gatesToCompute)
            {
                gi.Outputs.ExecuteActionForEachPin(pin => { Program.Write(gi.Name + ": "); pin.Redirect(); });
            }
        }

        public void DoFirstInitialization()
        {
            // Initialize implicit 0 and 1 pins
            Inputs[Inputs.Count - 2].Value = false;
            Inputs[Inputs.Count - 1].Value = true;

            foreach (var gi in GateInstances.Values)
            {
                gi.Initialize();
            }
        }

        public void DoRegularInitialization()
        {
            foreach (var gi in GateInstances.Values)
            {
                gi.IsModified = false;
            }
        }

        public void SetInputs(bool?[] inputValues)
        {
            for (int i = 0; i < inputValues.Length; ++i)
            {
                Inputs[i].Value = inputValues[i];
            }

            // Send inputs of network to connected pins
            Inputs.ExecuteActionForEachPin(pin => pin.Redirect());
        }
    }

    public class PinsCollection
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

        public void AddPin(IPin pin)
        {
            pins.Add(pin);

            ++Count;
        }

        public bool RegisterPin(IPin pin)
        {
            if (!pins.Contains(pin))
            {
                AddPin(pin);

                return true;
            }

            return false;
        }

        public void ExecuteActionForEachPin(Action<IPin> action)
        {
            pins.ForEach(pin => action(pin));
        }

        public bool?[] GetValues()
        {
            return pins.Select(pin => pin.Value).ToArray();
        }

        public bool Remove(IPin pin)
        {
            return pins.Remove(pin);
        }
    }

    class TransitionFunction
    {
        public bool?[] Inputs { get; private set; }
        public bool?[] Outputs { get; private set; }

        public TransitionFunction(bool?[] inputs, bool?[] outputs)
        {
            Inputs = inputs;
            Outputs = outputs;
        }

        public bool Matches(bool?[] definition)
        {
            for (int i = 0; i < Inputs.Length; ++i)
            {
                if (definition[i] != Inputs[i])
                    return false;
            }

            return true;
        }

        public bool Matches(TransitionFunction function)
        {
            return Matches(function.Inputs);
        }
    }

    public class Pin : IPin
    {
        public Action OnValueChangedAction { get; set; }

        public string Name { get; set; }

        protected bool? _value = null;
        public bool? Value 
        { 
            get { return _value; }
            set
            {
                if (_value != value)
                {
                    //Program.WriteLine(String.Format("\tPin {0}: Value changed from '{1}' to '{2}'", Name, _value, value));

                    _value = value;

                    if (OnValueChangedAction != null)
                    {
                        OnValueChangedAction();
                    }
                }
            }
        }

        public PinsCollection RedirectTo { get; set; }

        public Pin(string name)
        {
            Name = name;
        }

        public bool RegisterPinForRedirect(IPin pin)
        {
            if (RedirectTo == null)
                RedirectTo = new PinsCollection();

            return RedirectTo.RegisterPin(pin);
        }

        public void Redirect()
        {
            if (RedirectTo != null)
            {
                RedirectTo.ExecuteActionForEachPin(pin => {
                    if (Value != pin.Value)
                    {
                        //Program.WriteLine(String.Format("Redirecting '{0}' from {1} to {2}", Value, Name, pin.Name));
                        pin.Value = Value;
                    }
                });
            }
        }
    }

    public interface IPin
    {
        string Name { get; set; }

        bool? Value { get; set; }

        Action OnValueChangedAction { get; set; }

        PinsCollection RedirectTo { get; set; }

        bool RegisterPinForRedirect(IPin pin);

        void Redirect();
    }

    class NetworkException : Exception
    {
        public NetworkException(string message) : base(message) { }
    }

    class SyntaxException : NetworkException
    {
        public SyntaxException(int line) : base(String.Format("Line {0}: Syntax error.", line)) { }

        public SyntaxException() : base("Syntax error.") { }
    }

    class BindingException : NetworkException
    {
        public BindingException(int line) : base(String.Format("Line {0}: Binding rule.", line)) { }
    }

    class DuplicateException : NetworkException
    {
        public DuplicateException(int line) : base(String.Format("Line {0}: Duplicate.", line)) { }
    }

    class KeywordException : NetworkException
    {
        public KeywordException(int line) : base(String.Format("Line {0}: Missing keyword.", line)) { }
    }

    public static class Utils 
    {
        public static string[] SplitBySpaces(this string s)
        {
            return s.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
        }
    }
}
