using System;
using System.IO;
using System.Threading;
using System.Collections.Generic;
using System.Diagnostics;
using LuminousVector.Events;
using LuminousVector.DataStore;
using LuminousVector.Serialization;
using LuminousVector.Karuta.Commands;
using System.Reflection;
using System.Linq;

namespace LuminousVector.Karuta
{
	public static class Karuta
	{
		public static Registry REGISTY { get { return _registry; } }
		public static Logger LOGGER {  get { return _logger; } }
		public static Random RANDOM { get { return _random; } }
		public static string USER
		{
			set
			{
				_registry.SetValue("user", value);
				_user = value;
			}
			get
			{
				if (string.IsNullOrWhiteSpace(_user))
					return _registry.GetString("user");
				else
					return _user;
			}
		}
		public static readonly string DATA_DIR = @"/Karuta";

		internal static List<CommandIdentity> commandIDs { get { return _interpretor.commandIDs; } }

		private static Random _random;
		private static Logger _logger;
		private static Registry _registry;
		private static EventManager eventManager;
		private static string _user;
		private static bool _isRunning = false;
		private static string _input;
		private static Dictionary<string, Thread> _threads;
		private static Dictionary<string, Timer> _timers;
		private static CommandInterpreter<Command> _interpretor;

		//Initialize
		static Karuta()
		{
			//Prepare Console
			try
			{
				Console.Title = "Karuta";
				Console.BackgroundColor = ConsoleColor.DarkMagenta;
				Console.ForegroundColor = ConsoleColor.White;
				Console.Clear();
			}catch(Exception e)
			{
				Write(e.StackTrace);
			}
			Write("Preparing Karuta...");
			Stopwatch sw = new Stopwatch();
			sw.Start();
			//Init Vars
			_threads = new Dictionary<string, Thread>();
			_timers = new Dictionary<string, Timer>();
			_interpretor = new CommandInterpreter<Command>();
			_logger = new Logger();
			_random = new Random();
			//Init Event Manager
			eventManager = new EventManager();
			eventManager.Init();
			//Init Registry
			if(File.Exists($@"{DATA_DIR}/karuta.data"))
			{
				Write("Loading Registry...", false);
				_registry = Registry.Load($@"{DATA_DIR}/karuta.data");
				//_registry.Migrate();
				Write(" Done!");
			}
			else
			{
				_registry = new Registry();
				_registry.Init();
				_registry.SetValue("user", "user");
			}

			try
			{
				//Register Commands
				RegisterCommands();
				LoadPlugins();
				//Initalize commands
				Write("Initializing processes...", false);
				_interpretor.Init();
				Write(" Done!");
			}catch(Exception e)
			{
				Write($"An error occured while initializing commands: {e.Message}");
				Write(e.StackTrace);
			}

			sw.Stop();
			long elapsedT = sw.ElapsedTicks / (Stopwatch.Frequency / (1000L));

			Write($"Karuta is ready. Finished in {elapsedT}ms");
			LOGGER.Log($"Karuta started in {elapsedT}ms", "Karuta");
		}

		//Register all Commands
		private static void RegisterCommands()
		{
			//Find all commands with attribute KarutaCommand
			var cmds = from c in Assembly.GetExecutingAssembly().GetTypes()
					   where c.GetCustomAttributes<KarutaCommand>().Count() > 0
					   select c;
			//Add each register each command
			foreach(var c in cmds)
			{
				_interpretor.RegisterCommand(Activator.CreateInstance(c) as Command);
			}
			//Register Other Commands
			_interpretor.RegisterCommand(new Command("stop", Close, "stops all commands and closes Karuta."));
			_interpretor.RegisterCommand(new Command("clear", Console.Clear, "Clears the screen."));
			_interpretor.RegisterCommand(new Command("save", () =>
			{
				_registry.Save($@"{DATA_DIR}/karuta.data");
				Write("Registry saved");
			}, "Save the registry"));
			_interpretor.RegisterCommand(new ProcessMonitor(_threads, _timers));
		}

		//Load Plugins
		private static void LoadPlugins()
		{
			try
			{
				Write("Loading Plugins...", false);
				if (Directory.Exists("Plugins"))
				{
					foreach (string p in Directory.GetFiles("Plugins", "*.dll", SearchOption.TopDirectoryOnly))
					{
						var cmds = from c in Assembly.LoadFrom(p).GetTypes()
								   where c.GetCustomAttributes<KarutaCommand>().Count() > 0
								   select c;
						foreach (var c in cmds)
						{
							_interpretor.RegisterDynamicCommand(Activator.CreateInstance(c));
						}
					}
				}
			}catch(Exception e)
			{
				Write($"Failed to load plugin: {e.Message}");
				Write(e.StackTrace);
			}
			Write(" Done!");
		}
		
		//Start the main process loop
		public static void Run()
		{
			Write("Karuta is running...");
			_isRunning = true;
			while (_isRunning)
			{
				Console.Write($"{USER}: ");
				_input = Console.ReadLine();
				if (_input == null)
					continue;
				try
				{
					_interpretor.Interpret(_input);
				}catch (Exception e)
				{
					Write($"An error occured while interpreting the command: {e.Message}");
					Write(e.StackTrace);
				}
			}
		}

		//Stop the program, stops processes and dispose dispoables
		private static void Close()
		{
			_isRunning = false;
			_interpretor.Stop();
			foreach(Thread t in _threads.Values)
			{
				t.Abort();
			}
			foreach (Timer t in _timers.Values)
				t.Dispose();
			LOGGER.Dump();
			_registry.Save($@"{DATA_DIR}/karuta.data");
			Console.WriteLine("GoodBye");
		}

		//Say message with a name label
		public static void Say(string message)
		{
			Console.WriteLine($"Karuta: {message}");
		}

		//Say a message without name label
		public static void Write(object message, bool newLine = true)
		{
			if (newLine)
				Console.WriteLine(message);
			else
				Console.Write(message);
		}

		//Get an text input from Karuta's console
		public static string GetInput(string message, bool hide = false)
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
		public static Thread CreateThread(string name, Action thread)
		{
			Thread newThread = new Thread(() =>
			{
				try
				{
					thread?.Invoke();
				}
				catch (Exception e)
				{
					Write($"Something went wrong in {name}");
					Write(e.Message);
					Write(e.StackTrace);
				}
			});
			newThread.Name = $"Karuta.{name}";
			_threads.Add(newThread.Name, newThread);
			newThread.Start();
			return newThread;
		}

		//Force Join a Thread
		public static void ForceJoinThread(string name)
		{
			string tName = $"Karuta.{name}";
			if (_threads.ContainsKey(tName))
			{
				_threads[tName].Join();
			}
		}

		//Close Thread
		public static void CloseThread(Thread thread)
		{
			if (_threads.ContainsValue(thread))
			{
				thread.Abort();
				_threads.Remove(thread.Name);
			}
		}

		//Starts a timer
		public static Timer StartTimer(string name, Action<object> callback, int delay, int interval)
		{
			if (_timers.ContainsKey(name))
				throw new DuplicateTimerExeception(name);
			Timer timer = new Timer(i =>
			{
				try
				{
					callback?.Invoke(i);
				}
				catch (Exception e)
				{
					Write($"Something went wrong in {name}");
					Write(e.Message);
					Write(e.StackTrace);
				}
			}, null, delay, interval);
			_timers.Add(name, timer);
			return timer;
		}


		//Stops a timer
		public static void StopTimer(string name)
		{
			if (!_timers.ContainsKey(name))
				throw new NoSuchTimerExeception(name);
			_timers[name].Dispose();
			_timers.Remove(name);
		}

		//Invoke a command
		public static void InvokeCommand(string command, List<string> args) => _interpretor.Invoke(command, args);

	}
}
