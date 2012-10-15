using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace HashAlgorithms.Hashtables.Base
{
    class HashtableBase<TTableRow>
    {
        /// <summary>
        /// The table that contains rows and key under which rows are hashed under
        /// </summary>
        protected Dictionary<int, TTableRow> table;

        /// <summary>
        /// Defines whether enalrging table is allowed
        /// </summary>
        protected bool CanEnlargeTable { protected get; protected set; }

        /// <summary>
        /// Defines whether shrinking table is allowed
        /// </summary>
        protected bool CanShrinkTable { protected get; protected set; }

        /// <summary>
        /// Defines maximal count of hashed keys contained in the table
        /// </summary>
        protected int MaxCapacity { protected get; protected set; }

        /// <summary>
        /// Gets the level of occupancy
        /// </summary>
        public double Aplha 
        {
            get
            {
                double maxCapacity = MaxCapacity;
                double currentCapacity = table.Keys.Count;

                return currentCapacity / maxCapacity;
            }
        }

        /// <summary>
        /// Creates hash table with specified maximal hased keys capacity
        /// </summary>
        /// <param name="capacity"></param>
        public HashtableBase(int capacity)
        {
            MaxCapacity = capacity;
            table = new Dictionary<int, TTableRow>(MaxCapacity);
        }

        /// <summary>
        /// Inserts row in the table. 
        /// 
        /// Methd base defines algorithm:
        /// 1) hash key
        /// 2) while cannot add row under hashed value, repeat 1) with increased iteration counter
        /// 3) free place is found, store row under hashed value
        /// </summary>
        /// <param name="key">The key of the row</param>
        /// <param name="row">The row to insert into the table</param>
        /// <returns>Returns hash code of the row</returns>
        public virtual int Insert(int key, TTableRow row)
        {            
            CheckForEnlarge();

            int hashCode = FindFreeIndex(key); 

            // Hash row
            AddRow(hashCode, row);

            return hashCode;
        }

        private int FindFreeIndex(int key)
        {
            int hashCode = HashKey(key);

            // Find the place for the row
            int iteration = 1;
            while (!CanAddRow(hashCode))
            {
                hashCode = HashKey(key, ++iteration);
            }
            return hashCode;
        }

        /// <summary>
        /// Determines whether a row with specified key is hashed in the table
        /// </summary>
        /// <param name="key">Key of the row to find in the table</param>
        /// <returns>Returns true if key is hashed int the table</returns>
        public virtual bool Member(int key)
        {
            return table.ContainsKey(HashKey(key));
        }

        /// <summary>
        /// Deletes row with specified key from the table
        /// </summary>
        /// <param name="key">Key of the row to delete</param>
        /// <returns>Returns true if row was deleted, false if not</returns>
        public virtual bool Delete(int key)
        {
            // Not a member, nothing to delete
            if (!Member(key))
                return false;

            // Check whether the table is not empty enough
            CheckForShrink();

            // Remove row
            table.Remove(HashKey(key));

            return true;
        }

        /// <summary>
        /// Checks that table is not over-fulled
        /// </summary>
        protected void CheckForEnlarge()
        {
            if (CanEnlargeTable && table.Keys.Count == MaxCapacity)
                EnlargeTable();
        }

        /// <summary>
        /// Table is too small, enlarge
        /// </summary>
        protected void EnlargeTable()
        {
            // Resize table twice
            MaxCapacity *= 2;

            ResizeTable();
        }

        /// <summary>
        /// Resize table to a size given in the MaxCapacity property
        /// </summary>
        protected void ResizeTable()
        {
            // Remember old table
            Dictionary<int, TTableRow> oldTable = table;

            // Create new table
            table = new Dictionary<int, TTableRow>(MaxCapacity);

            // Rehash table
            foreach (int key in oldTable.Keys)
            {
                Insert(key, oldTable[key]);
            }
        }

        /// <summary>
        /// Check whether table is not over-sizes
        /// </summary>
        protected void CheckForShrink()
        {
            if (CanShrinkTable && table.Keys.Count <= MaxCapacity / 2)
                ShrinkTable();
        }

        /// <summary>
        /// Shrinks table
        /// </summary>
        protected void ShrinkTable()
        {
            // Resize table
            MaxCapacity /= 2;

            ResizeTable();
        }
        /// <summary>
        /// Adds row under proper hashed key
        /// </summary>
        /// <param name="hashCode">Hashed key for given row</param>
        /// <param name="row">Hashed row</param>
        protected void AddRow(int hashCode, TTableRow row)
        {
            table.Add(hashCode, row);
        }

        /// <summary>
        /// Colision detector - can a new row be added under this key?
        /// </summary>
        /// <param name="hashCode">Table row is hashed under this key</param>
        /// <returns>Returns true if row can be hashed under this code, false if not.</returns>
        protected bool CanAddRow(int hashCode)
        {
            return table.ContainsKey(hashCode);
        }

        /// <summary>
        /// Hash function
        /// </summary>
        /// <param name="key">Key to be hashed</param>
        /// <returns>Returns hased value of the key</returns>
        protected int HashKey(int key, int iteration = 0)
        {
            return (key + iteration) % MaxCapacity;
        }
    }
}
