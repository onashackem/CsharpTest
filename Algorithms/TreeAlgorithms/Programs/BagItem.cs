using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TreeAlgorithms.Programs
{
    class BagItem
    {
        public int Price { get; private set; }

        public int Weight { get; private set; }

        public string Name { get; private set; }

        public BagItem(int price, int weigth, string name)
        {
            Price = price;
            Weight = weigth;
            Name = name;
        }
    }
}
