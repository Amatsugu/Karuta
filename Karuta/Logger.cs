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
		private string src = null, search = null;
		private IList<Log> logs;
		private int count { get { return logs.Count; } }

		public Logs() : base("logs", "shows the logs.")
		{
			_default = ShowLogs;
			RegisterKeyword("dump", DumpLogs);
			RegisterOption('f', f => file = f, "Specify the file name, for log dump");
			RegisterOption('p', p => src = p.ToLower(), "Limit results to a specified program/command");
			RegisterOption('s', s => search = s.ToLower(), "Limit results to a specifiied search term");
			//RegisterOption('c', c => int.TryParse(c, out count), "Limit the ammount of results");
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
			logs = Karuta.logger.logs.ToList();

			//if (!string.IsNullOrWhiteSpace(src))
			//	logs = (from l in logs where l.source.ToLower() == src select l).ToList();
			if (!string.IsNullOrWhiteSpace(search))
				logs = (from l in logs where l.ToString().ToLower().Contains(search) select l).ToList();
			if (logs.Count == 0)
				Karuta.Write("No results found");
			else
			{
				foreach (string s in from l in Karuta.logger.logs select l.ToString())
					Karuta.Write(s);
			}
			Karuta.Write("---LOG END---");
			logs = null;
			search = src = null;
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
		private readonly int logLimit = 100000;

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

		public Logger Log(string message, string src, bool verbose = false)
		{
			if (startTime == default(DateTime))
				startTime = DateTime.Now;
			DateTime now = DateTime.Now;
			Log l = new Log()
			{
				time = DateTime.Now,
				threadName = Thread.CurrentThread.Name,
				source = src,
				logType = LogType.Info,
				message = message
			};
			log.Add(l);
			if (verbose)
				Karuta.Write(l);
			if (logs.Count >= logLimit)
				Dump();
			return this;
		}

		public Logger LogWarning(string message, string src, bool verbose = false)
		{
			if (startTime == null)
				startTime = DateTime.Now;
			DateTime now = DateTime.Now;
			Log l = new Log()
			{
				time = DateTime.Now,
				threadName = Thread.CurrentThread.Name,
				source = src,
				logType = LogType.Warn,
				message = message
			};
			log.Add(l);
			if (verbose)
				Karuta.Write(l);
			if (logs.Count >= logLimit)
				Dump();
			return this;
		}

		public Logger LogError(string message, string src, bool verbose = false)
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
			if (logs.Count >= logLimit)
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
