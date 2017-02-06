using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace LuminousVector.Karuta.Commands
{
	class ProcessMonitor : Command
	{
		private Dictionary<string, Thread> _threads;
		private Dictionary<string, Timer> _timers;
		private readonly string appName = "ThreadMonitor";

		public ProcessMonitor(Dictionary<string, Thread> threadList, Dictionary<string, Timer> timers) : base("processes", "lists and manages threads and timers")
		{
			_threads = threadList;
			_timers = timers;
			_default = ListAll;

			List<Thread> removalQ = new List<Thread>();

			init = () =>
			{
				//Start Thread Monitor
				Karuta.StartTimer("ThreadMonitor", e =>
				{
					removalQ.Clear();
					removalQ.AddRange(from thread in _threads.Values where thread.ThreadState == ThreadState.Aborted || thread.ThreadState == ThreadState.Stopped select thread);
					foreach (Thread thread in removalQ)
					{
						Karuta.LOGGER.Log($"Thread \"{thread.Name}\" has been {thread.ThreadState} and will be removed", appName);
						Karuta.CloseThread(thread);
					}
				}, 5 * 1000, 5 * 1000);
			};
		}

		void ListAll()
		{
			if(_threads.Count != 0)
			{
				Karuta.Write("\tThreads:");
				Karuta.Write("\t\tName \t\t Thread State");
				foreach(Thread t in _threads.Values)
					Karuta.Write($"\t\t{t.Name}\t\t{t.ThreadState}");
			}

			if(_timers.Count != 0)
			{
				Karuta.Write("\tTimers:");
				foreach (string t in _timers.Keys)
					Karuta.Write($"\t\t{t}");

			}
		}
	}
}
