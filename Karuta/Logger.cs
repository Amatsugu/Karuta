using System;
using System.IO;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Linq;
using System.Text;
using System.Threading;
using LuminousVector.Karuta.Commands;

namespace LuminousVector.Karuta
{
	[KarutaCommand(Name = "logs")]
	public class Logs : Command
	{
		private string file = null;

		public Logs() : base("logs", "shows the logs.")
		{
			_default = ShowLogs;
			RegisterKeyword("dump", DumpLogs);
			RegisterOption('f', f => file = f);
		}

		void ShowLogs()
		{
			file = null;
			if (Karuta.logger.logs.Count == 0)
			{
				Karuta.Write("There are currently no logs...");
				return;
			}
			Karuta.Write("---LOG START---");
			foreach (string s in from l in Karuta.logger.logs select l.ToString())
				Karuta.Write(s);
			Karuta.Write("---LOG END---");
		}

		void DumpLogs()
		{
			if (file != null)
			{
				Karuta.logger.Dump(file);
			}
			else
				Karuta.logger.Dump();
			file = null;
		}
	}

	public class Logger
	{
		public List<Log> logs { get { return log; } }
		private DateTime startTime;

		private List<Log> log;

		public Logger()
		{
			log = new List<Log>();
			SetupLogDir();
		}

		public void SetupLogDir()
		{
			if (!Directory.Exists(Karuta.dataDir + "/Logs/"))
				Directory.CreateDirectory(Karuta.dataDir + "/Logs/");
		}

		public Logger Log(string message, string src)
		{
			return Log(message, src, false);
		}

		public Logger LogWarning(string message, string src)
		{
			return LogWarning(message, src, false);
		}

		public Logger LogError(string message, string src)
		{
			return LogError(message, src, false);
		}

		public Logger Log(string message, string src, bool verbose)
		{
			if (startTime == default(DateTime))
				startTime = DateTime.Now;
			DateTime now = DateTime.Now;
			Log l = new Log()
			{
				time = DateTime.Now,
				threadName = Thread.CurrentThread.Name,
				source = src,
				logType = LogType.Error,
				message = message
			};
			log.Add(l);
			if (verbose)
				Karuta.Write(l);
			if (logs.Count >= 1000)
				Dump();
			return this;
		}

		public Logger LogWarning(string message, string src, bool verbose)
		{
			if (startTime == null)
				startTime = DateTime.Now;
			DateTime now = DateTime.Now;
			Log l = new Log()
			{
				time = DateTime.Now,
				threadName = Thread.CurrentThread.Name,
				source = src,
				logType = LogType.Error,
				message = message
			};
			log.Add(l);
			if (verbose)
				Karuta.Write(l);
			if (logs.Count >= 1000)
				Dump();
			return this;
		}

		public Logger LogError(string message, string src, bool verbose)
		{
			if (startTime == null)
				startTime = DateTime.Now;
			Log l = new Log()
			{
				time = DateTime.Now,
				threadName = Thread.CurrentThread.Name,
				source = src,
				logType = LogType.Error,
				message = message
			};
			log.Add(l);
			if (verbose)
				Karuta.Write(l);
			if (logs.Count >= 1000)
				Dump();
			return this;
		}

		public Logger Dump()
		{
			DateTime endTime = DateTime.Now;
			string file = $"{startTime.ToShortDateString()} {startTime.ToShortTimeString()} -- {endTime.ToShortDateString()} {endTime.ToShortTimeString()} log.txt";
			file = Regex.Replace(file, "/", "-");
			file = Regex.Replace(file, ":", ".");
			return Dump(Karuta.dataDir + "/Logs/" + file);
		}

		public Logger Dump(string file)
		{
			if (log.Count == 0)
				return this;
			File.WriteAllLines(file, from l in log select l.ToString());
			log.Clear();
			startTime = default(DateTime);
			return this;
		}
	}

	public enum LogType
	{
		Info, Warn, Error
	}

	public class Log
	{
		public string message { get; set; }
		public DateTime time { get; set; }
		public string source { get; set; }
		public string threadName { get; set; }
		public LogType logType { get; set; }

		public override string ToString()
		{
			return $"[{time.ToShortDateString()} {time.ToShortTimeString()}] [{threadName}] ({source}) {logType.ToString()}: {message}"; ;
		}

	}
}
