using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CodEx;

namespace SimpleSum
{
    class Program
    {
        static void Main(string[] args)
        {
            int numberCount = Convert.ToInt32(Reader.Console().Line());

            int sum = 0;
            for (int i = 0; i < numberCount; ++i)
            {
                sum += Reader.Console().Int();
            }

            Console.WriteLine(sum);
        }
    }
}
