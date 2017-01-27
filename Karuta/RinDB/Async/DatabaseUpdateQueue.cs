using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LuminousVector.Karuta.RinDB.Async
{
	public static class DatabaseUpdateQueue
	{
		private static Queue<string> _DB_UPDATE_QUEUE;
		private static bool _IS_RUNNING = false;

		static DatabaseUpdateQueue()
		{
			_DB_UPDATE_QUEUE = new Queue<string>();
			Karuta.StartTimer("RinDB.DatabaseQueue", i => 
			{
				if (_IS_RUNNING)
					return;
				_IS_RUNNING = true;
				string query = "";
				while(_DB_UPDATE_QUEUE.Count != 0)
					query += $"{_DB_UPDATE_QUEUE.Dequeue()};";
				//RinDB.Execute
			}, 0, 500);
		}
		

		public static void QueueCommand(string commandText)
		{
			_DB_UPDATE_QUEUE.Enqueue(commandText);
		}

	}
}
