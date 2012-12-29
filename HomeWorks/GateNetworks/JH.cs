//defining debug allows extended debug output to Console
//program also waits for any key before exiting
#define debug
#undef debug

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace Gates
{

	interface IInstanceableIOItem
	{
		IOItem MakeInstance(string name, ref Dictionary<Pin, Pin> subst);
	}

	interface IInstanceablePin
	{
		Pin MakeInstance(IOItem owner, string name);
	}

	class Program
	{
		//program makes one network instance
		static Network network = null;

		//many gates instances, which represents types of gates identified by name
		static Dictionary<string, Gate> gatesList = new Dictionary<string, Gate>();

		//can make Composite instances, which represents types of composites identified by name
		static Dictionary<string, Composite> compositeList = new Dictionary<string, Composite>();

		//we need to split words on almost every line... but own reader would be more efficient
		static char[] separators = new char[] { ' ' };

		//processed gates wait in queue
		public static Queue<Gate> GlobalQueue = new Queue<Gate>();

		//Kind of queue for SetValue Pin mechanism
		public static Dictionary<Pin, bool?> SetList = new Dictionary<Pin, bool?>();

		protected static int _cycle;

		/// <summary>
		/// Returns current cycle number of the network
		/// </summary>
		public static int CurrentCycle
		{
			get
			{
				return _cycle;
			}
		}

		static void Main(string[] args)
		{

			//params count control
			if (args.Length != 1)
			{
				Exit("Wrong parameters count.");
			}
			else {
				//params count OK, try to read the text file
				StreamReader inputStreamReader = null;

				try
				{
					FileStream inputFileStream = new FileStream(args[0], FileMode.Open, FileAccess.Read, FileShare.Read);
					inputStreamReader = new StreamReader(inputFileStream);
				}
				catch (Exception e)
				{
					Exit("Program was unable to read input file. Reason: " + e.Message);
				}

				//input file proccesed OK
				if (inputStreamReader != null && ProccessInputFile(inputStreamReader))
				{

					network.PrepareForFirstCycle();
					network.Initialize();

					string line;
					//read input data for current network
					while ((line = Console.ReadLine()) != "end")
					{

						string[] parameters = Tokenize(line);

						//check if we have only 0 and 1 in params
						bool inputOk = true;
						foreach (string p in parameters)
						{
							if (p != "0" && p != "1")
							{
								inputOk = false;
							}
						}

						if (!inputOk)
						{
							Console.WriteLine("Syntax error.");
							continue;
						}

						if (parameters.Length == network.InputsCount)
						{

							//set every input for network pin
							long i = 0;
							foreach (string key in network.Inputs.Keys)
							{
								network.Inputs[key].SetValueByString(parameters[i]);
								++i;
							}

							//Run 1 000 000 cycles or less

							try
							{
								_cycle = 0;
								while (GlobalQueue.Count > 0 && _cycle < 1000000)
								{
#if debug
									Console.WriteLine("==== Start of cycle "+_cycle+". ====");
#endif
									TimerTick(ref _cycle);
#if debug
									Console.WriteLine("==== End of cycle "+_cycle+". ====");
#endif
									++_cycle;
								}

								//write output data
								Console.Write("{0} ", _cycle);

								long o = 0;
								foreach (string key in network.Outputs.Keys)
								{
									Console.Write(network.Outputs[key].Value);
									++o;
									if (o != network.Outputs.Count)
									{
										Console.Write(" ");
									}
									else
									{
										Console.WriteLine();
									}
								}

							}
							catch (Exception e)
							{
								Console.WriteLine("Fatal error. "+e.Message);
							}

						}
						else
						{
							Console.WriteLine("Syntax error.");
						}
					}

					//User wants to exit the program
					Exit();
				}
				else
				{
					WriteError("The given input file wasn't proccessed. Please, check your syntax and try again.");
				}
			}

		}

		/// <summary>
		/// Adds pin into SetList queue (if it is in the list already, overwrites value)
		/// </summary>
		/// <param name="p">Pin which has to be set</param>
		/// <param name="value">Value for the pin</param>
		public static void AddToSetList(Pin p, bool? value)
		{
			if (SetList.ContainsKey(p))
			{
				SetList[p] = value;
			}
			else {
				SetList.Add(p, value);
			}
		}

		/// <summary>
		/// Represents one cycle in the network
		/// Checks all enqueued items. If they are ready, run compute(),
		/// otherwise put them back into the queue
		/// </summary>
		static void TimerTick(ref int cycle)
		{
			long cnt = GlobalQueue.Count;
			for (long i = 0; i < cnt; ++i)
			{
				Gate item = GlobalQueue.Dequeue();
				if (item.IsReady)
				{
					//Console.WriteLine("Cyklus" + cycle + ": pocitam polozku " + item);
					item.Compute();
				}
				else
				{
					item.CheckForCycles();
					if (!item.IsReady)
					{
						GlobalQueue.Enqueue(item);
					}
				}
			}

			//apply SetList values
			foreach (Pin p in Program.SetList.Keys)
			{
				p.SetValue(Program.SetList[p]);
			}

			Program.SetList.Clear();

		}

		/// <summary>
		/// Stops the application
		/// </summary>
		static void Exit()
		{
			#if debug
				Console.ReadKey();
			#endif

			Environment.Exit(0);
		}

		/// <summary>
		/// Stops the application after writing a reason to Console
		/// </summary>
		/// <param name="Reason"></param>
		static void Exit(string Reason)
		{
			WriteError(Reason);
			
			#if debug
				Console.ReadKey();
			#endif

			Environment.Exit(0);
		}

		/// <summary>
		/// Alias for Console.Write()
		/// </summary>
		/// <param name="errMsg"></param>
		static void WriteError(string errMsg)
		{
			Console.Write(errMsg);
		}

		/// <summary>
		/// Write Syntax Error to Console using override
		/// </summary>
		/// <param name="lineNumber"></param>
		public static void WriteSyntaxError(long lineNumber)
		{
			WriteSyntaxError(lineNumber, "Syntax error.");
		}

		/// <summary>
		/// Write Syntax Error to Console using Exit()
		/// </summary>
		/// <param name="lineNumber"></param>
		/// <param name="details"></param>
		static void WriteSyntaxError(long lineNumber, string details)
		{
			Exit("Line " + lineNumber.ToString() + ": " + details);
		}

		/// <summary>
		/// Reads the input file
		/// </summary>
		/// <param name="reader"></param>
		/// <param name="net"></param>
		/// <returns></returns>
		static bool ProccessInputFile(StreamReader reader)
		{
			long lineNumber = 0;

			//read input file
			string line;
			while ((line = reader.ReadLine()) != null)
			{
				++lineNumber;

				// check rules for lines which should be ignored
				if (EmptyLine(line))
				{
					continue;
				}

				string[] data = Tokenize(line);

				switch (data[0])
				{
					case "gate":
						ReadGate(reader, ref lineNumber, data);
						break;
					case "composite":
						ReadComposite(reader, ref lineNumber, data, false);
						break;
					case "network":
						if (network != null)
						{
							WriteDuplicate(lineNumber);
						}
						else
						{
							ReadComposite(reader, ref lineNumber, data, true);	
						}
						break;
					default:
						if (!EmptyLine(line)) WriteSyntaxError(lineNumber);
						break;
				}


			}

			//gatescount > 0 && 1 network (and similiar checks)
			performCheck(lineNumber);
			
			//Why there is no return false? Because every syntax error exits the application...
			return true;
		}

		/// <summary>
		/// Splits line into an array (global separators)
		/// </summary>
		/// <param name="line"></param>
		/// <returns></returns>
		static string[] Tokenize(string line)
		{
			return line.Split(separators);
		}

		/// <summary>
		/// Reads gate definition
		/// </summary>
		/// <param name="reader"></param>
		/// <param name="lineNumber"></param>
		/// <param name="data"></param>
		static void ReadGate(StreamReader reader, ref long lineNumber, string[] data)
		{

			//line 1: gate keyword + name => |line 1| = 2
			if (data.Length != 2) WriteSyntaxError(lineNumber);

			ValidateName(data[1], lineNumber);

			//if this type already exists, something is wrong... with the user :-)
			if (gatesList.ContainsKey(data[1])) WriteDuplicate(lineNumber);

			string gateName = data[1];
			Gate g = new Gate(gateName);

			ReadInputsAndOutputs(reader, ref lineNumber, g);

			string line;
			while (reader.Peek() != -1 && (line = reader.ReadLine()) != "end")
			{

				++lineNumber;
				if (EmptyLine(line))
				{
					continue;
				}

				data = Tokenize(line);
				if (data.Length != g.InputsCount + g.OutputsCount) WriteBrokenBindingRule(lineNumber);
				
				//Well, this is not very clear... but simple. It would be better to use own data structure
				//based on bool?[] as a key for every binding rule, but it costs own implementation of
				//ICompareable
				StringBuilder s = new StringBuilder();
				for (long i = 0; i < g.InputsCount; ++i)
				{
					s.Append(data[i].ToString() + " ");
				}

				//get output for given input
				bool?[] output = new bool?[g.OutputsCount];
				for (long i = 0; i < g.OutputsCount; ++i)
				{
					output[i] = strToNullableBool(data[i+g.InputsCount], lineNumber);
				}

				//add binding rule
				g.AddBindingRule(s.ToString().Trim(), output);
			}
			++lineNumber;

			//add gate type into list
			gatesList.Add(gateName, g);

		}

		/// <summary>
		/// Reads composite definition
		/// </summary>
		/// <param name="reader"></param>
		/// <param name="lineNumber"></param>
		/// <param name="data"></param>
		static void ReadComposite(StreamReader reader, ref long lineNumber, string[] data, bool isNetwork)
		{
			CompositeItem c;
			string compositeName = null;
			
			//composite gate
			if (!isNetwork)
			{
				if (data.Length != 2) WriteSyntaxError(lineNumber);

				ValidateName(data[1], lineNumber);
				if (compositeList.ContainsKey(data[1])) WriteDuplicate(lineNumber);

				compositeName = data[1];
				c = new Composite(compositeName);
			}
			//network
			else {
				c = network = new Network("network"); //the name is passed only for debugging (
			}

			ReadInputsAndOutputs(reader, ref lineNumber, c);

			string line = skipEmptyLines(reader, ref lineNumber);
			while (reader.Peek() != -1 && (line != "end" && line.StartsWith("gate")))
			{
				//if (EmptyLine(line)) continue; //no need to have it here beacause of the line.StartsWith()

				data = Tokenize(line);

				//check tokens count (gate instance_name gate|composite_type => 3)
				if (data.Length != 3) WriteSyntaxError(lineNumber);

				//is the instance name correct?
				ValidateName(data[1], lineNumber);

				//if the composite already owns instance with this name, exit with synerr
				if (c.HasItem(data[1])) WriteDuplicate(lineNumber);

				//instance of gate or instance of another composite?
				//not very strict... allows to have composite type with the same name as gate type
				//switch the priority between gate and composite by changing the order of the following conditions
				Dictionary<Pin, Pin> subst = new Dictionary<Pin, Pin>();
				if (gatesList.ContainsKey(data[2]))
				{
					c.AddItemInstance(data[1], (Gate)gatesList[data[2]].MakeInstance(data[1], ref subst));
				}
				else if (compositeList.ContainsKey(data[2]))
				{
					c.AddItemInstance(data[1], (Composite) compositeList[data[2]].MakeInstance(data[1], ref subst));
				}else
				{
					WriteSyntaxError(lineNumber);
				}

				line = reader.ReadLine();
				++lineNumber;

			}
			//++lineNumber;

			//read given binding rules
			ReadBindingRules(reader, ref lineNumber, c, line);

			//Every output has to be wired
			foreach (Output o in c.Outputs.Values)
			{
				if (!o.IsWired)
				{
					WriteBrokenBindingRule(lineNumber);
				}
			}

			//Every input of network has to have at least one follower
			if (isNetwork)
			{
				foreach (Input i in c.Inputs.Values)
				{
					if (i.HasFollower == false)
					{
						WriteBrokenBindingRule(lineNumber);
					}
				}
			}

			//every composite has to be composed from something :-)
			if (c.ItemsCount == 0) WriteMissingKeyword(lineNumber);

			if (!isNetwork)
			{
				compositeList.Add(compositeName, (Composite) c);
			}
		}

		/// <summary>
		/// Read binding rules from definition file stream
		/// </summary>
		/// <param name="reader"></param>
		/// <param name="lineNumber"></param>
		/// <param name="i"></param>
		/// <param name="line"></param>
		static void ReadBindingRules(StreamReader reader, ref long lineNumber, CompositeItem i, string line)
		{
			#if debug
			Console.WriteLine("Binding rules start on line " + (lineNumber));
			#endif

			while (reader.Peek() != -1 && line != "end")
			{
				if (EmptyLine(line))
				{
					line = reader.ReadLine();
					++lineNumber;
					continue;
				}
				
				#if debug
				Console.WriteLine(line+" "+lineNumber);
				#endif

				//find -> in binding rule
				int pos = line.IndexOf("->");
				//if there isn't -> or it is on the beggining of the line, write Syntax Error...
				if (pos < 1) WriteSyntaxError(lineNumber);

				//split by ->
				string first = line.Substring(0, pos);
				string second = line.Substring(pos + 2); //-> has two chars, not only one :)

				if (first.Contains("."))
				{
					Pin fol = i.GetInnerItem(first.Substring(0, first.IndexOf('.')), lineNumber).GetInputByName(first.Substring(first.IndexOf('.') + 1), lineNumber);
					if (second.Contains("."))
					{
						i.GetInnerItem(second.Substring(0, second.IndexOf('.')), lineNumber).GetOutputByName(second.Substring(second.IndexOf('.') + 1), lineNumber).AddFollower(fol);
					}
					else if (second == "0") //Input 0
					{
						((Input)fol).SetValue(false, false);
					}
					else if (second == "1") //Input 1
					{
						((Input)fol).SetValue(true, false);
					}
					else
					{
						i.GetInputByName(second, lineNumber).AddFollower(fol);
					}
				}
				else
				{
					if (!second.Contains(".")) WriteBrokenBindingRule(lineNumber);

					Output o = i.GetOutputByName(first, lineNumber);

					if (!o.IsWired)
					{
						i.GetInnerItem(second.Substring(0, second.IndexOf('.')), lineNumber).GetOutputByName(second.Substring(second.IndexOf('.') + 1), lineNumber).AddFollower(o);
						o.IsWired = true;
					}
					else
					{
						WriteBrokenBindingRule(lineNumber);
					}
				}

				line = reader.ReadLine();
				++lineNumber;
			}

		}

		/// <summary>
		/// Translates string to nullable bool - (1 => true, 0 => false, ? => null)
		/// </summary>
		/// <param name="s"></param>
		/// <param name="lineNumber"></param>
		/// <returns></returns>
		static bool? strToNullableBool(string s, long lineNumber)
		{
			switch (s)
			{
				case "0":
					return false;
				case "1":
					return true;
				case "?":
					return null;
				default:
					WriteSyntaxError(lineNumber);
					break;
			}
			return null;
		}

		/// <summary>
		/// Reads inputs and outputs definiton
		/// </summary>
		/// <param name="reader"></param>
		/// <param name="lineNumber"></param>
		/// <param name="item"></param>
		static void ReadInputsAndOutputs(StreamReader reader, ref long lineNumber, IOItem item)
		{
			string line = skipEmptyLines(reader, ref lineNumber);
			string[] data = Tokenize(line);

			if (data[0] == "inputs")
			{
				for (int i = 1; i < data.Length; ++i)
				{
					ValidateName(data[i], lineNumber);
					item.AddInput(new Input(data[i], item), data[i]);
				}

				line = skipEmptyLines(reader, ref lineNumber);
				data = Tokenize(line);

				if (data.Length == 1) WriteMissingKeyword(lineNumber);

				if (data[0] == "outputs")
				{
					for (int i = 1; i < data.Length; ++i)
					{
						ValidateName(data[i], lineNumber);
						item.AddOutput(new Output(data[i], item), data[i]);
					}
				}
				else
				{
					WriteMissingKeyword(lineNumber);
				}
			}
			else
			{
				WriteMissingKeyword(lineNumber);
			}
		}

		/// <summary>
		/// Detects empty lines in stream and skips them
		/// </summary>
		/// <param name="reader"></param>
		/// <param name="lineNumber"></param>
		/// <returns></returns>
		static string skipEmptyLines(StreamReader reader, ref long lineNumber)
		{
			return skipEmptyLines(reader, ref lineNumber, true);
		}

		/// <summary>
		/// Detects empty lines in stream and skips them,
		/// ends app with syntax error when non-empty line is found (strict must be set to true)
		/// </summary>
		/// <param name="reader"></param>
		/// <param name="lineNumber"></param>
		/// <param name="strict"></param>
		/// <returns></returns>
		static string skipEmptyLines(StreamReader reader, ref long lineNumber, bool strict)
		{
			string line = null;
			while (EmptyLine(line) && reader.Peek() != -1 && line != "end")
			{
				line = reader.ReadLine();
				++lineNumber;
			}
			if (strict && line == null)
			{
				++lineNumber;
				WriteMissingKeyword(lineNumber);
			}
			return line;
		}

		/// <summary>
		/// Writes Line N: Missing keyword. and terminates the appliaction
		/// </summary>
		/// <param name="lineNumber"></param>
		static void WriteMissingKeyword(long lineNumber)
		{
			WriteSyntaxError(lineNumber, "Missing keyword.");
		}

		/// <summary>
		/// Writes Line N: Dupliacte. and terminates the application
		/// </summary>
		/// <param name="lineNumber"></param>
		static void WriteDuplicate(long lineNumber)
		{
			WriteSyntaxError(lineNumber, "Duplicate.");
		}

		static void WriteBrokenBindingRule(long lineNumber)
		{
			WriteSyntaxError(lineNumber, "Binding rule broken.");
		}

		/// <summary>
		/// Writes syntax error when the given name is not valid
		/// </summary>
		/// <param name="name"></param>
		/// <param name="lineNumber"></param>
		static void ValidateName(string name, long lineNumber)
		{
			if (name.Contains(".") || name.Contains("->") || name.Contains(";") || name.StartsWith("end") || name.Contains('\t') || name.Contains(Convert.ToChar(10)))
			{
				WriteSyntaxError(lineNumber);
			}
		}

		/// <summary>
		/// Checks if the given line is empty (or null, or comment line begining with ";"
		/// string.Length property is used because of its implementation in MSIL
		/// </summary>
		/// <param name="line"></param>
		/// <returns></returns>
		static bool EmptyLine(string line)
		{
			if (line == null || line.Length == 0 || line.Trim().Length == 0 || line.Substring(0, 1) == ";")
			{
				return true;
			}
			return false;
		}

		/// <summary>
		/// Cheks if we have a network and at least one gate
		/// </summary>
		/// <param name="lineNumber"></param>
		static void performCheck(long lineNumber)
		{
			if (network == null)
			{
				WriteSyntaxError(lineNumber);
			}

			if (gatesList.Count == 0)
			{
				WriteSyntaxError(lineNumber);
			}
		}

	}
	
	/// <summary>
	/// Abstract for objects with inputs and outputs
	/// </summary>
	abstract class IOItem : IInstanceableIOItem
	{
		//Dictionaries with inputs and outputs
		protected Dictionary<string, Input> _Inputs = new Dictionary<string, Input>();
		protected Dictionary<string, Output> _Outputs = new Dictionary<string, Output>();

		public abstract bool IsReady { get; }
		public abstract IOItem MakeInstance(string name, ref Dictionary<Pin, Pin> subst);
		public abstract void SubstituteFollowers(Dictionary<Pin, Pin> subst);

		public string name;

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="name">Item name</param>
		public IOItem(string name)
		{
			this.name = name;
			
			#if debug
			Console.WriteLine("Constructor of IOItem with name {0} was called.", name);
			#endif
		}

		/// <summary>
		/// Add new input
		/// </summary>
		/// <param name="i"></param>
		/// <param name="key"></param>
		public void AddInput(Input i, string key)
		{
			if (!_Inputs.ContainsKey(key))
			{
				_Inputs.Add(key, i);
			}
		}

		/// <summary>
		/// Add new output
		/// </summary>
		/// <param name="o"></param>
		/// <param name="key"></param>
		public void AddOutput(Output o, string key)
		{
			if (!_Outputs.ContainsKey(key))
			{
				_Outputs.Add(key, o);
			}
		}

		/// <summary>
		/// Get amount of inputs
		/// </summary>
		public long InputsCount
		{
			get { return _Inputs.Count; }	
		}

		/// <summary>
		/// Get amount of outputs
		/// </summary>
		public long OutputsCount
		{
			get	{ return _Outputs.Count; }
		}

		/// <summary>
		/// Get item inputs
		/// </summary>
		public Dictionary<string, Input> Inputs
		{
			get { return _Inputs; }
		}

		/// <summary>
		/// Get item outputs
		/// </summary>
		public Dictionary<string, Output> Outputs
		{
			get { return _Outputs; }
		}

		/// <summary>
		/// Get specific input by given name
		/// </summary>
		/// <param name="name">name</param>
		/// <param name="lineNumber">line of the textfile which requires this input</param>
		/// <returns></returns>
		public Input GetInputByName(string name, long lineNumber)
		{
			if (this._Inputs.ContainsKey(name))
			{
				return this._Inputs[name];
			}
			else
			{
				Program.WriteSyntaxError(lineNumber);
				return null;
			}
		}

		/// <summary>
		/// Get specific output by given name
		/// </summary>
		/// <param name="name">name</param>
		/// <param name="lineNumber">line of the textfile which requires this output</param>
		public Output GetOutputByName(string name, long lineNumber)
		{
			if (this._Outputs.ContainsKey(name))
			{
				return this._Outputs[name];
			}
			else
			{
				Program.WriteSyntaxError(lineNumber);
				return null;
			}
		}

		/// <summary>
		/// ToString() - returns name of the item
		/// </summary>
		/// <returns></returns>
		public override string ToString()
		{
			return name;
		}

		/// <summary>
		/// Item initialization
		/// </summary>
		abstract public void Initialize();
		/*virtual public void Initialize()
		{
			/*foreach (Input i in _Inputs.Values)
			{
				i.Initialize();
			}

			foreach (Output o in _Outputs.Values)
			{
				o.Initialize();
			}*

		}*/

		public abstract void PrepareForFirstCycle();
	}

	/// <summary>
	/// Abstract class for composite items
	/// </summary>
	abstract class CompositeItem : IOItem
	{
		protected Dictionary<string, IOItem> _items = new Dictionary<string, IOItem>();

		/// <summary>
		/// Constructor (calls IOItem(name))
		/// </summary>
		/// <param name="name">item name</param>
		public CompositeItem(string name) : base(name) { }

		/// <summary>
		/// Add new item into the items list
		/// </summary>
		/// <param name="name"></param>
		/// <param name="g"></param>
		public void AddItemInstance(string name, IOItem g)
		{
			if (!_items.ContainsKey(name))
			{
				_items.Add(name, g);
			}
		}

		/// <summary>
		/// Check if the composite has item with given name
		/// </summary>
		/// <param name="name"></param>
		/// <returns></returns>
		public bool HasItem(string name)
		{
			return _items.ContainsKey(name);
		}

		/// <summary>
		/// Get items count
		/// </summary>
		public long ItemsCount
		{
			get { return _items.Count; }
		}

		/// <summary>
		/// Is the item ready for computing?
		/// </summary>
		public override bool IsReady
		{
			get
			{
				foreach (string key in _Inputs.Keys)
				{
					if (!_Inputs[key].IsSet) return false;
				}

				return true;
			}
		}

		/// <summary>
		/// Get Item by given name
		/// </summary>
		/// <param name="name">item name</param>
		/// <param name="lineNumber">linenumber of the textfile which requires the item</param>
		/// <returns></returns>
		public IOItem GetInnerItem(string name, long lineNumber)
		{
			if (_items.ContainsKey(name))
			{
				return _items[name];
			}
			else
			{
				Program.WriteSyntaxError(lineNumber);
			}

			return null;
		}

		/// <summary>
		/// Item initialization
		/// </summary>
		public override void Initialize()
		{
			/*foreach (Input i in _Inputs.Values)
			{
				i.Initialize();
			}

			foreach (Output o in _Outputs.Values)
			{
				o.Initialize();
			}*/

			foreach (IOItem i in _items.Values)
			{
				//i.Initialize();
				if (i.InputsCount == 0)
				{
					i.Initialize();
				}

				bool fols = false;
				foreach (Input inp in i.Inputs.Values)
				{
					if (inp.HasFollower)
					{
						fols = true;
						break;
					}
				}

				if (!fols)
				{
					i.Initialize();
				}

			}

		}
		
	}

	/// <summary>
	/// Network class
	/// </summary>
	class Network : CompositeItem
	{

		public Network(string name) : base(name) { }

		/// <summary>
		/// Is the network ready for compute?
		/// </summary>
		public override bool IsReady
		{
			get
			{
				return base.IsReady;
			}
		}

		/// <summary>
		/// Unable to clone network. Returns "singleton" instance
		/// </summary>
		/// <param name="name"></param>
		/// <param name="subst"></param>
		/// <returns></returns>
		public override IOItem MakeInstance(string name, ref Dictionary<Pin, Pin> subst)
		{
			return this;
		}

		public override void SubstituteFollowers(Dictionary<Pin, Pin> subst)
		{
			//has no sense?
		}

		/// <summary>
		/// Get ready before the first run of the network
		/// </summary>
		public override void PrepareForFirstCycle()
		{
			foreach (IOItem i in _items.Values)
			{
				i.PrepareForFirstCycle();
			}
		}
	}

	/// <summary>
	/// Composite class
	/// </summary>
	class Composite : CompositeItem, IInstanceableIOItem
	{

		public Composite(string name) : base(name) { }

		/// <summary>
		/// Makes a clone for the composite instance. Used when e.g. we need to use more instances
		/// of the same composite gate type in our network.
		/// </summary>
		/// <returns></returns>
		public override IOItem MakeInstance(string name, ref Dictionary<Pin, Pin> subst)
		{
			Composite c = new Composite(name);
			foreach (string key in _items.Keys)
			{
				c.AddItemInstance(key, _items[key].MakeInstance(key, ref subst));
			}

			foreach (string key in _Inputs.Keys)
			{
				Input oldInput = (Input) _Inputs[key];
				Input newInput = (Input) oldInput.MakeInstance(c, key);
				c.AddInput(newInput, key);

				subst.Add(oldInput, newInput);
			}

			foreach (string key in _Outputs.Keys)
			{
				Output oldOutput = (Output) _Outputs[key];
				Output newOutput = (Output) oldOutput.MakeInstance(c, key);
				c.AddOutput(newOutput, key);

				subst.Add(oldOutput, newOutput);
			}

			c.SubstituteFollowers(subst);

			return c;
		}

		/// <summary>
		/// After making instance of composite item, we need to update binding rules between inner items
		/// </summary>
		/// <param name="subst"></param>
		public override void SubstituteFollowers(Dictionary<Pin, Pin> subst)
		{
			foreach (Input i in _Inputs.Values)
			{
				foreach (Pin p in i.Followers.ToArray())
				{
					if (subst.ContainsKey(p))
					{
						i.Followers.Remove(p);
						i.Followers.Add(subst[p]);
					}
				}
			}

			foreach (Output o in _Outputs.Values)
			{
				foreach (Pin p in o.Followers.ToArray())
				{
					if (subst.ContainsKey(p))
					{
						o.Followers.Remove(p);
						o.Followers.Add(subst[p]);
					}
				}
			}

			foreach (IOItem i in _items.Values)
			{
				i.SubstituteFollowers(subst);
			}

		}

		/// <summary>
		/// Is the item ready to compute?
		/// </summary>
		public override bool IsReady
		{
			get
			{
				return base.IsReady;
			}
		}

		/// <summary>
		/// Prepare for the firs run of the item
		/// </summary>
		public override void PrepareForFirstCycle()
		{
			foreach (IOItem i in _items.Values)
			{
				i.PrepareForFirstCycle();
			}
		}
	}

	class Gate : IOItem, IInstanceableIOItem
	{
		protected Dictionary<string, bool?[]> _bindingRules = new Dictionary<string,bool?[]>();
		protected bool _proccessed = false;

		public Gate(string name) : base(name) { }

		public void AddBindingRule(string input, bool?[] output)
		{
			_bindingRules.Add(input, output);
		}

		/// <summary>
		/// Makes a clone for the gate instance. Used when e.g. we need to use more instances
		/// of the same gate type in a composite gate or in a network.
		/// </summary>
		/// <returns></returns>
		public override IOItem MakeInstance(string name, ref Dictionary<Pin, Pin> subst)
		{
			// create new instance of Gate
			Gate g = new Gate(name);

			// copy all binding rules
			foreach (string key in _bindingRules.Keys)
			{
				g.AddBindingRule(key, _bindingRules[key]);
			}

			// clone all inputs and copy them into the new instance
			foreach (string key in _Inputs.Keys)
			{
				Input oldInput = (Input)_Inputs[key];
				Input newInput = (Input)oldInput.MakeInstance(g, key);
				g.AddInput(newInput, key);

				subst.Add(oldInput, newInput);
			}

			// clone all outputs and copy them into the new instance
			foreach (string key in _Outputs.Keys)
			{
				Output oldOutput = (Output)_Outputs[key];
				Output newOutput = (Output)oldOutput.MakeInstance(g, key);
				g.AddOutput(newOutput, key);

				subst.Add(oldOutput, newOutput);
			}

			// return new instance
			return g;
		}

		/// <summary>
		/// Repair binding rules
		/// </summary>
		/// <param name="subst"></param>
		public override void  SubstituteFollowers(Dictionary<Pin,Pin> subst)
		{
			foreach (Input i in _Inputs.Values)
			{
				foreach (Pin p in i.Followers.ToArray())
				{
					if (subst.ContainsKey(p))
					{
						i.Followers.Remove(p);
						i.Followers.Add(subst[p]);
					}
				}
			}

			foreach (Output o in _Outputs.Values)
			{
				foreach (Pin p in o.Followers.ToArray())
				{
					if (subst.ContainsKey(p))
					{
						o.Followers.Remove(p);
						o.Followers.Add(subst[p]);
					}
				}
			}
		}

		/// <summary>
		/// Get binding rules
		/// </summary>
		public Dictionary<string, bool?[]> BindingRules
		{
			get { return _bindingRules; }
		}

		/// <summary>
		/// Compute - set outputs in dependence on inputs
		/// </summary>
		public void Compute()
		{

#if debug
	Console.WriteLine("Computing item "+this);
#endif

			StringBuilder s = new StringBuilder();
			long c = 0;
			foreach (string key in _Inputs.Keys)
			{
				s.Append(_Inputs[key].Value);
				++c;
				if (c < _Inputs.Count) s.Append(" ");
			}

			string input = s.ToString();

			//if a binding ruler for current input is specified
			if (_bindingRules.ContainsKey(input))
			{
				long i = 0;
				foreach (string key in _Outputs.Keys)
				{
					//_Outputs[key].SetValue(_bindingRules[input][i]); //no, no, no...
					Program.AddToSetList((Pin)_Outputs[key], _bindingRules[input][i]);
					++i;
				}
			}
			//if it's not
			else
			{
				if (input.Contains("?"))
				{
					long i = 0;
					foreach (string key in _Outputs.Keys)
					{
						//_Outputs[key].SetValue(null);
						Program.AddToSetList((Pin)_Outputs[key], null);
						++i;
					}
				}
				else
				{
					long i = 0;
					foreach (string key in _Outputs.Keys)
					{
						//_Outputs[key].SetValue(false);
						Program.AddToSetList((Pin)_Outputs[key], false);
						++i;
					}
				}
			}

		}

		/// <summary>
		/// It the item ready for computing?
		/// </summary>
		public override bool IsReady
		{
			get {

				foreach (string key in _Inputs.Keys)
				{
					if (!_Inputs[key].IsSet) return false;
				}

				return true;
			}
		}

		/// <summary>
		/// Detect cycles in the network. Is it possible to send signal throughg followers and get it back on any input?
		/// </summary>
		/// <returns></returns>
		public bool CheckForCycles()
		{
			bool ret = false;

			foreach (Input i in Inputs.Values)
			{
				if (!i.IsSet)
				{
					foreach (Output o in Outputs.Values)
					{
						if (o.FindPin(i))
						{
							i.SetValue(null);
							ret = true;
							break;
						}
					}
				}
			}

			// if there was found a cycle, input was set to ? and we need to check, if the item is ready now
			// if it is, run compute()
			if (IsReady)
			{
				//Console.WriteLine("Sam od sebe se spocitam. Jmenuji se" + this);
				Compute();
			}

			return ret;
		}

		/// <summary>
		/// Prepare for the first run - set outputs by given rules ? if input count > 0, value given by binding rule otherwise
		/// </summary>
		public override void PrepareForFirstCycle()
		{
			if (this.InputsCount == 0)
			{
				long output = 0;
				foreach (Output o in _Outputs.Values)
				{
					o.SetValue(_bindingRules[""][output]);
					++output;
				}
			}
		}

		public override void Initialize()
		{
			if (_Inputs.Count > 0)
			{
				foreach (Input i in _Inputs.Values)
				{
					i.SetValue(null);
				}
				Program.GlobalQueue.Enqueue(this);
			}
		}

	}

	/// <summary>
	/// Abstract class for Pins (Inputs and Outputs)
	/// </summary>
	abstract class Pin
	{
		protected string _name;
		protected IOItem _owner;
		protected bool? _value;
		protected bool _hasValue = false;

		//Pins which like to know current pin's value
		protected List<Pin> _followers = new List<Pin>();

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="name">Pin name - only for better debug</param>
		/// <param name="owner">Pin owner</param>
		public Pin(string name, IOItem owner)
		{
			_name = name;
			_owner = owner;
		}

		/// <summary>
		/// Get Pin owner
		/// </summary>
		public IOItem Owner
		{
			get {
				return _owner;
			}
		}

		/// <summary>
		/// Add follower
		/// </summary>
		/// <param name="p"></param>
		public void AddFollower(Pin p)
		{
			if (!_followers.Contains(p))
			{
				_followers.Add(p);
			}
		}

		/// <summary>
		/// Get list of followers
		/// </summary>
		public List<Pin> Followers
		{
			get {
				return _followers;
			}
		}

		/// <summary>
		/// Get value as a string
		/// </summary>
		public string Value {
			get {
				switch (_value)
				{
					case true:
						return "1";
					case false:
						return "0";
					case null:
						return "?";
					default:
						return "?";
				}
			}
		}

		/// <summary>
		/// Set value by bool?
		/// </summary>
		/// <param name="value"></param>
		public virtual void SetValue(bool? value)
		{
			_value = value;
			_hasValue = true;

			#if debug
			Console.WriteLine("Setting "+this.GetType().ToString()+" "+_name+" of owner "+Owner+" to value "+_value.ToString());
			#endif

			if (_followers.Count > 0)
			{
				this.SetFollowers();
			}

		}

		/// <summary>
		/// Set value by string
		/// </summary>
		/// <param name="value"></param>
		public void SetValueByString(string value)
		{
			switch (value)
			{
				case "?":
					_value = null;
					break;
				case "1":
					_value = true;
					break;
				case "0":
					_value = false;
					break;
				default:
					_value = null;
					break;
			}

			this._hasValue = true;
			this.SetFollowers();
		}

		/// <summary>
		/// Set value of the current pin for its followers
		/// </summary>
		protected void SetFollowers()
		{
			foreach (Pin p in Followers)
			{
				#if debug
				//Console.Write("Follower: ");
				#endif
				p.SetValue(_value);
			}
		}

		/// <summary>
		/// Do we have any follower?
		/// </summary>
		public bool HasFollower
		{
			get
			{
				if (_followers.Count > 0) return true;

				return false;
			}
		}

		/// <summary>
		/// Do we have any value set?
		/// </summary>
		public bool IsSet
		{
			get
			{
				return _hasValue;
			}
		}

		/// <summary>
		/// Pretend, there is no value set
		/// </summary>
		public void Initialize()
		{
			this._hasValue = false;
		}

	}

	class Input : Pin, IInstanceablePin
	{
		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="name"></param>
		/// <param name="owner"></param>
		public Input(string name, IOItem owner)
			: base(name, owner)
		{
			
		}

		/// <summary>
		/// Clone Input
		/// </summary>
		/// <param name="owner"></param>
		/// <param name="name"></param>
		/// <returns></returns>
		public Pin MakeInstance(IOItem owner, string name)
		{
			Input i = new Input(name, owner);
			
			foreach (Pin f in _followers)
			{
				i.AddFollower(f);
			}

			return i;
		}

		/// <summary>
		/// Set value to the given value
		/// </summary>
		/// <param name="value"></param>
		public override void SetValue(bool? value)
		{
			SetValue(value, true);
		}

		/// <summary>
		/// Set value
		/// </summary>
		/// <param name="value"></param>
		/// <param name="enqueue">Should be the owner added to GlobaQueue?</param>
		public void SetValue(bool? value, bool enqueue)
		{
			if (_value != value)
			{
				_value = value;

				#if debug
				Console.WriteLine("Setting " + this.GetType().ToString() + " " + _name + " of owner " + Owner + " to value " + _value.ToString());
				#endif

				if (_followers.Count > 0)
				{
					this.SetFollowers();
				}
				else if (enqueue && !Program.GlobalQueue.Contains((Gate)this.Owner))
				{
					Program.GlobalQueue.Enqueue((Gate)this.Owner);

					#if debug
					Console.WriteLine("{0} enqueued", this.Owner);
					//Console.ReadKey();
					#endif

				}
			}

			_hasValue = true;
		}

	}

	/// <summary>
	/// Output class
	/// </summary>
	class Output : Pin, IInstanceablePin
	{

		bool _wired = false;
		bool _openedByCycleDetection = false;

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="name"></param>
		/// <param name="owner"></param>
		public Output(string name, IOItem owner)
			: base(name, owner)
		{
			
		}

		/// <summary>
		/// Clone Output
		/// </summary>
		/// <param name="owner"></param>
		/// <param name="name"></param>
		/// <returns></returns>
		public Pin MakeInstance(IOItem owner, string name)
		{
			Output o = new Output(name, owner);
			foreach (Pin f in _followers)
			{
				o.AddFollower(f);
			}

			return o;
		}

		/// <summary>
		/// Is this output already opened by cycle detection mechanism? if it is, we found a cycle...
		/// </summary>
		public bool OpenedByCycleDetection
		{
			get
			{
				return _openedByCycleDetection;
			}
		}

		/// <summary>
		/// Try to find given input as a follower of owner's outputs
		/// </summary>
		/// <param name="i"></param>
		/// <returns></returns>
		public bool FindPin(Pin i)
		{
			//Console.WriteLine("Oteviram "+this._name+" na "+Owner);
			_openedByCycleDetection = true;
			foreach (Pin p in _followers)
			{
				if (p == i)
				{
					//Console.WriteLine("Zaviram " + this._name + " na " + Owner+" saham sam na sebe?");
					_openedByCycleDetection = false;
					return true;
				}

				if (p.Owner == i.Owner) return false;
				foreach (Output o in p.Owner.Outputs.Values)
				{
					if (!o.OpenedByCycleDetection)
					{
						if (o.FindPin(i))
						{
							//Console.WriteLine("Zaviram " + this._name + " na " + Owner+" nasel jsem ten input");
							_openedByCycleDetection = false;
							return true;
						}
					}
					else
					{
						//Console.WriteLine("Zaviram " + this._name + " na " + Owner+" cyklus o neco dal.");
						_openedByCycleDetection = false;
						return true;
					}
				}
			}
			//Console.WriteLine("Zaviram " + this._name + " na " + Owner);
			_openedByCycleDetection = false;
			return false;
		}

		/// <summary>
		/// 
		/// </summary>
		public bool IsWired
		{
			get
			{
				return _wired;
			}

			set
			{
				_wired = value;
			}
		}

	}
}