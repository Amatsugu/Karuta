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

		public ProcessMonitor(Dictionary<string, Thread> threadList, Dictionary<string, Timer> timers) : base("processes", "lists and manages threads and timers")
		{
			_threads = threadList;
			_timers = timers;
			_default = ListAll;

			init = () =>
			{
				//Start Thread Monitor
				Karuta.StartTimer("ThreadMonitor", e =>
				{
					List<Thread> removalQ = new List<Thread>();
					foreach (Thread thread in _threads.Values)
					{
						if (thread.ThreadState.Equals(System.Diagnostics.ThreadState.Terminated))
							removalQ.Add(thread);
					}
					foreach (Thread thread in removalQ)
					{
						_threads.Remove(thread.Name);
					}
					removalQ.Clear();
				}, 10 * 1000, 10 * 1000);
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
