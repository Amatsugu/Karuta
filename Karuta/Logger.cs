using System;
using System.IO;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Linq;
using System.Text;
using System.Threading;
using com.LuminousVector.Karuta.Commands;

namespace com.LuminousVector.Karuta
{
	public class Logs : Command
	{
		public Logs() : base("logs") { }

		private string file = null;

		protected override void Init()
		{
			base.Init();
			_helpMessage = "shows the logs.";
			_default = ShowLogs;
			RegisterKeyword("dump", DumpLogs);
			RegisterOption('f', SetFile);
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
			string[] logs = Karuta.logger.logs.ToArray();
			foreach (string s in logs)
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

		void SetFile(string file)
		{
			this.file = file;
		}
	}

	public class Logger
	{
		public List<string> logs { get { return log; } }
		private DateTime startTime;

		private List<string> log;

		public Logger()
		{
			log = new List<string>();
			if (!Directory.Exists(@"C:/Karuta/Logs/"))
				Directory.CreateDirectory(@"C:/Karuta/Logs/");
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
			string l = "[" + now.ToShortDateString() + " " + now.ToShortTimeString() + "] " + "[" + Thread.CurrentThread.Name + "] (" + src + ") INFO: " + message;
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
			string l = "[" + now.ToShortDateString() + " " + now.ToShortTimeString() + "] " + "[" + Thread.CurrentThread.Name + "] (" + src + ") WARN: " + message;
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
			DateTime now = DateTime.Now;
			string l = "[" + now.ToShortDateString() + " " + now.ToShortTimeString() + "] " + "[" + Thread.CurrentThread.Name + "] (" + src + ") ERROR: " + message;
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
			string file = startTime.ToShortDateString() + " " + startTime.ToShortTimeString() +" -- " + endTime.ToShortDateString() + " " + endTime.ToShortTimeString() + " log.txt";
			file = Regex.Replace(file, "/", "-");
			file = Regex.Replace(file, ":", ".");
			return Dump(@"C:/Karuta/Logs/" + file);
		}

		public Logger Dump(string file)
		{
			if (log.Count == 0)
				return this;
			//Karuta.Write(file);
			File.WriteAllLines(file, log.ToArray());
			log.Clear();
			startTime = default(DateTime);
			return this;
		}
	}
}
