using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace ThreadAbortRepeated {
	class Program {
		static void Main(string[] args) {

			var t = new Thread(() => {
				try {
					try {
						Console.WriteLine("Thread 2: Entering main infinite loop ...");
						while (true) ;
					} catch (ThreadAbortException) {
						Console.WriteLine("Thread 2: ThreadAbortException caught. Entering infinite loop ...");
						// Uncommenting leads to a deadlock below at second Thread.Abort!
						// Thread.ResetAbort();
						while (true) ;
					}
				} finally {
					Console.WriteLine("Thread 2: Outer finally block reached. Entering infinite loop ...");
					while (true) ;
				}
			});
			Console.WriteLine("Main: Second thread created, it's state: {0}", t.ThreadState);
			t.Start();
			Console.WriteLine("Main: Second thread started, it's state: {0}", t.ThreadState);

			Thread.Sleep(1000);
			Console.WriteLine("Main: 1st: Trying to abort second thread in state {0} ...", t.ThreadState);
			t.Abort();

			Thread.Sleep(1000);
			Console.WriteLine("Main: 2nd: Trying to abort second thread in state {0} ...", t.ThreadState);
			t.Abort();

			Thread.Sleep(1000);
			Console.WriteLine("Main: 3rd: Trying to abort second thread in state {0} ...", t.ThreadState);
			t.Abort();

			Thread.Sleep(1000);
			Console.WriteLine("Main: Second thread state: {0}", t.ThreadState);
			t.IsBackground = true;		// Force thread termination at process exit
		}
	}
}
