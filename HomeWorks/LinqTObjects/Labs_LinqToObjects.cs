using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Labs_LinqToObjects {
	class Program {
		static void Main(string[] args) {
			Console.WriteLine("Press ENTER to run without debug prints,");
			Console.WriteLine("Press D1 + ENTER to enable some debug prints,");
			Console.Write("Press D2 + ENTER to enable all debug prints: ");
			string command = Console.ReadLine().ToUpper();
			DebugPrints1 = command == "D2" || command == "D1" || command == "D";
			DebugPrints2 = command == "D2";
			Console.WriteLine();

			var groupA = new Group();

			HighlightedWriteLine("Assignment 1: Vsechny osoby, ktere nepovazuji nikoho za sveho pritele.");

            var a1 = from person in groupA where person.Friends.Count() == 0 select person;

            PrintQueryResult(a1);

			Console.WriteLine();
			HighlightedWriteLine("Assignment 2: Vsechny osoby setridene vzestupne podle jmena, ktere jsou starsi 15 let, a jejichz jmeno zacina na pismeno D nebo vetsi.");

            var b1 = from person in groupA where person.Age > 15 && (int)person.Name[0] >= (int)'D' orderby person.Name ascending select person;

            PrintQueryResult(b1);
            
			Console.WriteLine();
			HighlightedWriteLine("Assignment 3: Vsechny osoby, ktere jsou ve skupine nejstarsi, a jejichz jmeno zacina na pismeno T nebo vetsi.");

            var maxAge = groupA.Max(person => person.Age);
            var c1 = from person in groupA where person.Age == maxAge && (int)person.Name[0] >= (int)'T' select person;

            PrintQueryResult(c1);

			Console.WriteLine();
			HighlightedWriteLine("Assignment 4: Vsechny osoby, ktere jsou starsi nez vsichni jejich pratele.");

            var d1 = from person in groupA where person.Friends.Count() == 0 || person.Age > person.Friends.Max(friend => friend.Age) select person;

            PrintQueryResult(d1);

			Console.WriteLine();
			HighlightedWriteLine("Assignment 5: Vsechny osoby, ktere nemaji zadne pratele (ktere nikoho nepovazuji za sveho pritele, a zaroven ktere nikdo jiny nepovazuje za sveho pritele).");

            var friendsOfSomeone = (from person in groupA from friend in person.Friends select friend).Distinct();
            var e1 = from person in groupA where person.Friends.Count() == 0 && !friendsOfSomeone.Contains(person) select person;

            PrintQueryResult(e1);

			Console.WriteLine();
			HighlightedWriteLine("Assignment 6: Vsechny osoby, ktere jsou necimi nejstarsimi prateli (s opakovanim).");

            var f1 = from person in groupA from friend in person.Friends where friend.Age == person.Friends.Max(f => f.Age) select friend;

            PrintQueryResult(f1);

            HighlightedWriteLine("\tAlternative solution:");

            var oldestFreindAges = from person in groupA where person.Friends.Count() > 0 select new { person, Age = person.Friends.Max(f => f.Age) };
            var f2 = from ofa in oldestFreindAges from friend in ofa.person.Friends where friend.Age == ofa.Age select friend;

            PrintQueryResult(f2);

			Console.WriteLine();
			HighlightedWriteLine("Assignment 6B: Vsechny osoby, ktere jsou necimi nejstarsimi prateli (bez opakovani).");

            PrintQueryResult(f1.Distinct());

			Console.WriteLine();
			HighlightedWriteLine("Assignment 7: Vsechny osoby, ktere jsou nejstarsimi prateli osoby starsi nez ony samy (s opakovanim).");

            var g1 = from person in groupA from friend in person.Friends where person.Age > friend.Age && friend.Age == person.Friends.Max(f => f.Age) select friend;
            
            PrintQueryResult(g1);

			Console.WriteLine();
			HighlightedWriteLine("Assignment 7B: Vsechny osoby, ktere jsou nejstarsimi prateli osoby starsi nez ony samy (bez opakovani).");

            PrintQueryResult(g1.Distinct());

			Console.WriteLine();
			HighlightedWriteLine("Assignment 7C: Vsechny osoby, ktere jsou nejstarsimi prateli osoby starsi nez ony samy (bez opakovani a setridene sestupne podle jmena osoby).");

            PrintQueryResult(g1.Distinct().OrderByDescending(person => person.Name));
		}

		public static void HighlightedWriteLine(string s) {
			ConsoleColor oldColor = Console.ForegroundColor;
			Console.ForegroundColor = ConsoleColor.Green;
			Console.WriteLine(s);
			Console.ForegroundColor = oldColor;
		}

        private static void PrintQueryResult(IEnumerable<Person> queryResult)
        {
            foreach (var person in queryResult)
                Console.WriteLine(person);
        }

		public static bool DebugPrints1 = false;
		public static bool DebugPrints2 = false;

		class Person {
			public string Name { get; set; }
			public int Age { get; set; }
			public IEnumerable<Person> Friends { get; private set; }

			public IList<Person> FriendsListInternal { get; private set; }

			class EnumWrapper<T> : IEnumerable<T> {
				IEnumerable<T> innerEnumerable;
				Person person;
				string propName;

				public EnumWrapper(Person person, string propName, IEnumerable<T> innerEnumerable) {
					this.person = person;
					this.propName = propName;
					this.innerEnumerable = innerEnumerable;
				}

				public IEnumerator<T> GetEnumerator() {
					if (Program.DebugPrints1) Console.WriteLine(" # Person(\"{0}\").{1} is being enumerated.", person.Name, propName);

					foreach (var value in innerEnumerable) {
						yield return value;
					}

					if (Program.DebugPrints2) Console.WriteLine(" # All elements of Person(\"{0}\").{1} have been enumerated.", person.Name, propName);
				}

				System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() {
					return GetEnumerator();
				}
			}

			public Person() {
				FriendsListInternal = new List<Person>();
				Friends = new EnumWrapper<Person>(this, "Friends", FriendsListInternal);
			}

			public override string ToString() {
				return string.Format("Person(Name = \"{0}\", Age = {1})", Name, Age);
			}
		}

		class Group : IEnumerable<Person> {
			Person anna, blazena, ursula, daniela, emil, vendula, cyril, frantisek, hubert, gertruda;

			public Group() {
				anna = new Person { Name = "Anna", Age = 22 };
				blazena = new Person { Name = "Blazena", Age = 18 };
				ursula = new Person { Name = "Ursula", Age = 22, FriendsListInternal = { blazena } };
				daniela = new Person { Name = "Daniela", Age = 18, FriendsListInternal = { ursula } };
				emil = new Person { Name = "Emil", Age = 21 };
				vendula = new Person { Name = "Vendula", Age = 22, FriendsListInternal = { blazena, emil } };
				cyril = new Person { Name = "Cyril", Age = 21, FriendsListInternal = { daniela } };
				frantisek = new Person { Name = "Frantisek", Age = 15, FriendsListInternal = { anna, blazena, cyril, daniela, emil } };
				hubert = new Person { Name = "Hubert", Age = 10 };
				gertruda = new Person { Name = "Gertruda", Age = 10, FriendsListInternal = { frantisek } };

				blazena.FriendsListInternal.Add(ursula);
				blazena.FriendsListInternal.Add(vendula);
				ursula.FriendsListInternal.Add(daniela);
				daniela.FriendsListInternal.Add(cyril);
				emil.FriendsListInternal.Add(vendula);
			}

			public IEnumerator<Person> GetEnumerator() {
				if (Program.DebugPrints1) Console.WriteLine("*** Group is being enumerated.");

				yield return hubert;
				yield return anna;
				yield return frantisek;
				yield return blazena;
				yield return ursula;
				yield return daniela;
				yield return emil;
				yield return vendula;
				yield return cyril;
				yield return gertruda;

				if (Program.DebugPrints1) Console.WriteLine("*** All elements of Group have been enumerated.");
			}

			System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() {
				return GetEnumerator();
			}
		}
	}
}
