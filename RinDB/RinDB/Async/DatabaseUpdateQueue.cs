using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static LuminousVector.Karuta.Karuta;
using Npgsql;

namespace LuminousVector.RinDB.Async
{
	public static class DatabaseUpdateQueue
	{
		private static Queue<string> _DB_UPDATE_QUEUE;
		private static bool _IS_RUNNING = false;
		private static NpgsqlConnection _CONNECTION;

		static DatabaseUpdateQueue()
		{
			_DB_UPDATE_QUEUE = new Queue<string>();
			_CONNECTION = RinDB.GetConnection();
			StartTimer("RinDB.DatabaseQueue", i => 
			{
				if (_IS_RUNNING)
					return;
				if (_CONNECTION == null)
					return;
				_IS_RUNNING = true;
				string query = "";
				while(_DB_UPDATE_QUEUE.Count != 0)
					query += $"{_DB_UPDATE_QUEUE.Dequeue()};";
				using (var command = _CONNECTION.CreateCommand())
				{
					command.CommandText = query;
					command.ExecuteNonQuery();
				}
			}, 0, 500);
		}

		internal static void Close()
		{
			_CONNECTION.Dispose();
			_CONNECTION = null;
			StopTimer("RinDB.DatabaseQueue");
		}
		
		internal static void QueueCommand(string commandText)
		{
			_DB_UPDATE_QUEUE.Enqueue(commandText);
		}

	}
}
