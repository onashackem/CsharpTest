using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace SimpleProducerConsumer {
	class Program {
		static void Main(string[] args) {
			Queue<int> queue = new Queue<int>();

			var producerThread = new Thread(() => {
				for (int i = 0; i <= 10 ; i++) {
					lock (queue) {
						queue.Enqueue(i);
						Monitor.Pulse(queue);
					}

					Thread.Sleep(250);
				}
			});

			ThreadStart consumer = () => {
				while (true) {
					int item;

					lock (queue) {
						while (queue.Count == 0) Monitor.Wait(queue);
						item = queue.Peek();
						if (item == -1) break;	// Swallow the poison pill

						queue.Dequeue();
					}

					Console.WriteLine(string.Format("{0}: {1}", Thread.CurrentThread.ManagedThreadId, item));
				}
			};

			var consumerThread1 = new Thread(consumer);
			var consumerThread2 = new Thread(consumer);

			consumerThread1.Start();
			consumerThread2.Start();
			producerThread.Start();

			producerThread.Join();
			// Terminate all consumers
			lock (queue) {
				queue.Enqueue(-1);			// Prescribe a poison pill to all consumers
				Monitor.PulseAll(queue);
			}

			consumerThread1.Join();
			consumerThread2.Join();
		}
	}
}
