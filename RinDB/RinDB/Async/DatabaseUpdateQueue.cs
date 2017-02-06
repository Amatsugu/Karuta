using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static LuminousVector.Karuta.Karuta;
using Npgsql;

namespace LuminousVector.RinDB.Async
{
	public static class DBUpdateQueue
	{
		public static int QUEUE_LENGTH { get { return _DB_UPDATE_QUEUE.Count; } }
		public static int COMPLEDTED_COUNT { get; set; } = 0;
		public static bool IS_RUNNING { get { return _DB_UPDATE_QUEUE.Count > 0; } }

		private static Queue<string> _DB_UPDATE_QUEUE = new Queue<string>();
		private static bool _IS_RUNNING = false;
		private static NpgsqlConnection _CONNECTION;
		private static NpgsqlCommand _COMMAND;
		private static bool _JOB_DONE = false;

		internal static void Start()
		{
			StartTimer("RinDB.DatabaseQueue", i =>
			{
				if (_IS_RUNNING)
					return;
				if (_DB_UPDATE_QUEUE.Count == 0)
					return;
				if (_CONNECTION == null)
				{
					_CONNECTION = RinDB.GetConnection();
					_CONNECTION.Open();
					_COMMAND = _CONNECTION.CreateCommand();
				}
				//_CONNECTION.Open();
				_IS_RUNNING = true;
				string query = "";
				while (_DB_UPDATE_QUEUE.Count != 0 && query.Length <= 5000)
				{
					query += $"{_DB_UPDATE_QUEUE.Dequeue()};";
					COMPLEDTED_COUNT++;
				}
				_COMMAND.CommandText = query;
				_COMMAND.ExecuteNonQuery();
				if (!_JOB_DONE && _DB_UPDATE_QUEUE.Count == 0)
				{
					_JOB_DONE = true;
					Write("DB Updates Done!");
				}
				_IS_RUNNING = false;
			}, 0, 500);
		}

		internal static void Close()
		{
			_CONNECTION?.Dispose();
			_CONNECTION = null;
			StopTimer("RinDB.DatabaseQueue");
		}
		
		internal static void QueueCommand(string commandText)
		{
			_DB_UPDATE_QUEUE.Enqueue(commandText);
			_JOB_DONE = false;
		}

	}
}
