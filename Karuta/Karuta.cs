using System;
using System.IO;
using System.Threading;
using System.Collections.Generic;
using System.Speech.Synthesis;
using System.Diagnostics;
using com.LuminousVector.Events;
using com.LuminousVector.DataStore;
using com.LuminousVector.Serialization;

namespace com.LuminousVector.Karuta
{
	public static class Karuta
	{
		public static EventManager eventManager;
		public static Dictionary<string, Command> commands { get { return _commands; } }
		public static Registry registry;
		public static Logger logger;
		public static string user
		{
			set
			{
				registry.SetValue("user", value);
			}
			get
			{
				return registry.GetString("user");
			}
		}
		private static bool _isRunning = false;
		private static string _input;
		private static SpeechSynthesizer _voice;
		private static Dictionary<string,Command> _commands;
		private static List<Thread> _threads;
		static Karuta()
		{
			//Init Vars
			_commands = new Dictionary<string, Command>();
			_threads = new List<Thread>();
			logger = new Logger();
			//_threads.Add(Thread.CurrentThread);
			//Prepare Console
			Console.Title = "Karuta";
			Console.BackgroundColor = ConsoleColor.DarkMagenta;
			Console.ForegroundColor = ConsoleColor.White;
			Console.Clear();
			Stopwatch sw = new Stopwatch();
			sw.Start();
			Write("Preparing Karuta...");
			//Init Event Manager
			eventManager = new EventManager();
			eventManager.Init();
			//Init Registry
			if(File.Exists(@"C:/Karuta/karuta.data"))
			{
				registry = DataSerializer.deserializeData<Registry>(File.ReadAllBytes(@"C:/Karuta/karuta.data"));
			}else
			{
				registry = new Registry();
				registry.Init();
				registry.SetValue("user", "user");
			}
			//Prepare Voice
			_voice = new SpeechSynthesizer();
			_voice.SelectVoiceByHints(VoiceGender.Female, VoiceAge.Teen);
			//Register Commands
			RegisterCommand(new Command("stop", "Stop Karuta", Close));
			RegisterCommand(new Command("clear", "Clear the screen.", Console.Clear));
			RegisterCommand(new UserCommand());
			RegisterCommand(new HelpCommand());
			RegisterCommand(new DrawCommand());
			RegisterCommand(new PlexCommand());
			RegisterCommand(new LightingCommand());
			RegisterCommand(new RedditCrawler());
			RegisterCommand(new Logs());
			sw.Stop();
			long elapsedT = sw.ElapsedTicks / (Stopwatch.Frequency / (1000L));
			Write("Karuta is ready. Finished in " + elapsedT + "ms");
		}

		public static void RegisterCommand(Command command)
		{
			commands.Add(command.name, command);
		}

		public static void Run()
		{
			Write("Karuta is running...");
			_isRunning = true;
			while (_isRunning)
			{
				Console.Write(user + ": ");
				_input = Console.ReadLine();
				string[] args = _input.Split(' ');
				Command cmd;
				commands.TryGetValue(args[0], out cmd);
				if (cmd == null)
					Say(args[0]);
				else
					cmd.Run(args);
			}
		}

		//Stop the program and dispose dispoables
		public static void Close()
		{
			_isRunning = false;
			foreach(Command c in commands.Values)
			{
				c.Close();
			}
			_voice.Dispose();
			logger.Dump();
			File.WriteAllBytes(@"C:/Karuta/karuta.data", DataSerializer.serializeData(registry));
			Console.WriteLine("GoodBye");
		}

		//Say message with voice and name label
		public static void Say(string message)
		{
			_voice.SpeakAsyncCancelAll();
			_voice.SpeakAsync(message);
			Console.WriteLine("Karuta: " + message);
		}

		//Say a message without name label or voice
		public static void Write(string message)
		{
			Console.WriteLine(message);
		}

		//Get an text input from Karuta's console
		public static string GetInput(string message, bool hide)
		{
			Console.Write(message + ": ");
			if(hide == false)
				return Console.ReadLine();
			string input = "";
			ConsoleKeyInfo key;
			do
			{
				key = Console.ReadKey(true);

				if (key.Key != ConsoleKey.Backspace && key.Key != ConsoleKey.Enter)
				{
					input += key.KeyChar;
					Console.Write("*");
				}
				else
				{
					if (key.Key == ConsoleKey.Backspace && input.Length > 0)
					{
						input = input.Substring(0, (input.Length - 1));
						Console.Write("\b \b");
					}
				}
			}
			while (key.Key != ConsoleKey.Enter);
			Console.WriteLine();
			return input;
		}

		//Create a ChildThread
		public static Thread CreateThread(string name, ThreadStart thread)
		{
			Thread newThread = new Thread(thread);
			newThread.Name = "Karuta." + name;
			_threads.Add(newThread);
			newThread.Start();
			return newThread;
		}

		//Close Thread
		public static void RemoveThread(Thread thread)
		{
			if (_threads.Contains(thread))
				_threads.Remove(thread);
		}

		//Invoke a command
		public static void InvokeCommand(string command, string[] args)
		{
			if(commands.ContainsKey(command))
			{
				throw new NoSuchCommandException();
			}else
			{
				commands[command].Run(args);
			}
		}

	}
}
