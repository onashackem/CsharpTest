using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace LockAndValueTypes {
	class Program {
		static void Main(string[] args) {
			// Boxing problem: following does not work!

			DateTime time = new DateTime(2013, 03, 04);

			ThreadStart locker = () => {
				// Error: 'System.DateTime' is not a reference type as required by the lock statement
				// lock (time) {
				Monitor.Enter(time);
					Console.WriteLine("Thread {0}: Entered critical section.", Thread.CurrentThread.ManagedThreadId);
					Thread.Sleep(2000);
					Console.WriteLine("Thread {0}: Leaving critical section.", Thread.CurrentThread.ManagedThreadId);
				Monitor.Exit(time);		// Throws an exception as we are trying to unlock a lock not locked before
										// (we locked a lock associated with a completly different instance above)
			};

			var t1 = new Thread(locker);
			t1.Start();

			locker();
			t1.Join();

			// Boxing the value type once - works, but weird -> try to void it!

			Console.WriteLine();

			object time2 = time;
			ThreadStart locker2 = () => {
				Monitor.Enter(time2);
					Console.WriteLine("Thread {0}: Entered critical section.", Thread.CurrentThread.ManagedThreadId);
					Thread.Sleep(2000);
					Console.WriteLine("Thread {0}: Leaving critical section.", Thread.CurrentThread.ManagedThreadId);
				Monitor.Exit(time2);
			};

			var t2 = new Thread(locker2);
			t2.Start();

			locker2();
			t2.Join();
		}
	}
}
