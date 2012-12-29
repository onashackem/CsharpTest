using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Deque
{
    class Program
    {
        static void Main(string[] args)
        {
            Deque<int> deque = new Deque<int>();
            List<int> list = new List<int>();

            deque.Add(5);
            list.Add(1);
            list.Add(2);
            list.Add(3);
            list.Add(5);
            list.Insert(3, 4);
            Print("Add 5:", deque, list);           
        }

        private static void Print<T, U>(String message, Deque<T> deque, List<U> list)
        {
            Console.WriteLine(message);

            foreach (T x in deque)
            {
                Console.Write(x + ", ");
            }

            Console.WriteLine();

            foreach (U x in list)
            {
                Console.Write(x + ", ");
            }

            Console.WriteLine();
            Console.WriteLine("---------");
            Console.WriteLine();

            Console.ReadLine();
        }
    }
}
