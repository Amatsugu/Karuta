using System;
using System.IO;
using System.Threading;
using System.Collections.Generic;
using System.Diagnostics;
using LuminousVector.Events;
using LuminousVector.DataStore;
using LuminousVector.Serialization;
using LuminousVector.Karuta.Commands;
using LuminousVector.Karuta.Commands.DiscordBot;
using System.Reflection;
using System.Linq;

namespace LuminousVector.Karuta
{
	public static class Karuta
	{
		public static EventManager eventManager;
		public static Dictionary<string, Command> commands { get { return _interpretor.commands; } }
		public static Registry registry;
		public static Logger logger;
		public static Random random;
		public static string user
		{
			set
			{
				registry.SetValue("user", value);
				_user = value;
			}
			get
			{
				if (string.IsNullOrWhiteSpace(_user))
					return registry.GetValue<string>("user");
				else
					return _user;
			}
		}
		public static string dataDir
		{
			get
			{
				return _regDir;
			}
			set
			{
				_regDir = value;
				File.WriteAllBytes(dataDir + "/karuta.data", DataSerializer.serializeData(registry));
				logger.SetupLogDir();
			}
		}


		private static string _user;
		private static string _regDir = "/Karuta";
		private static bool _isRunning = false;
		private static string _input;
		private static Dictionary<string, Thread> _threads;
		private static Dictionary<string, Timer> _timers;
		private static CommandInterpreter<Command> _interpretor;

		//Initialize
		static Karuta()
		{
			//Init Vars
			_threads = new Dictionary<string, Thread>();
			_timers = new Dictionary<string, Timer>();
			_interpretor = new CommandInterpreter<Command>();
			logger = new Logger();
			random = new Random();
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
			Stopwatch sw = new Stopwatch();
			sw.Start();
			Write("Preparing Karuta...");
			//Init Event Manager
			eventManager = new EventManager();
			eventManager.Init();
			//Init Registry
			Write("Loading Registry...");
			if(File.Exists(dataDir + "/karuta.data"))
			{
				registry = DataSerializer.deserializeData<Registry>(File.ReadAllBytes(dataDir + "/karuta.data"));
				registry.Migrate();
			}else
			{
				registry = new Registry();
				registry.Init();
				registry.SetValue("user", "user");
			}

			try
			{
				//Register Commands
				RegisterCommands();
				//Initalize commands
				Write("Initializing processes...");
				_interpretor.Init();
			}catch(Exception e)
			{
				Write($"An error occured while initializing commands: {e.Message}");
				Write(e.StackTrace);
			}
			sw.Stop();
			long elapsedT = sw.ElapsedTicks / (Stopwatch.Frequency / (1000L));

			Write($"Karuta is ready. Finished in {elapsedT}ms");
			logger.Log($"Karuta started in {elapsedT}ms", "Karuta");
		}

		//Register all Commands
		public static void RegisterCommands()
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
				File.WriteAllBytes(dataDir + "/karuta.data", DataSerializer.serializeData(registry));
				Write("Registry saved");
			}, "Save the registry"));
			_interpretor.RegisterCommand(new ProcessMonitor(_threads, _timers));
		}

		//Start the main process loop
		public static void Run()
		{
			Write("Karuta is running...");
			_isRunning = true;
			while (_isRunning)
			{
				Console.Write($"{user}: ");
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
		public static void Close()
		{
			_isRunning = false;
			foreach(Command c in commands.Values)
			{
				c.Stop();
			}
			foreach(Thread t in _threads.Values)
			{
				t.Abort();
			}
			foreach (Timer t in _timers.Values)
				t.Dispose();
			logger.Dump();
			File.WriteAllBytes(dataDir + "/karuta.data", DataSerializer.serializeData(registry));
			Console.WriteLine("GoodBye");
		}

		//Say message with a name label
		public static void Say(string message)
		{
			Console.WriteLine($"Karuta: {message}");
		}

		//Say a message without name label
		public static void Write(Object message)
		{
			Console.WriteLine(message);
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
		public static Thread CreateThread(string name, ThreadStart thread)
		{
			Thread newThread = new Thread(AddExeceptionWrapper(thread, name));
			newThread.Name = $"Karuta.{name}";
			_threads.Add(newThread.Name, newThread);
			newThread.Start();
			return newThread;
		}

		//Wraps the thread start in a try-catch
		private static ThreadStart AddExeceptionWrapper(ThreadStart threadstart, string name)
		{
			return () =>
			{
				try
				{
					threadstart?.Invoke();
				}
				catch (Exception e)
				{
					Write($"Something went wrong in {name}");
					Write(e.Message);
					Write(e.StackTrace);
				}
			};
		}

		//Wrarps the timercallback in a try-catch
		private static TimerCallback AddExeceptionWrapper(Action<object> timer, string name)
		{
			return i =>
			{
				try
				{
					timer?.Invoke(i);
				}
				catch (Exception e)
				{
					Write($"Something went wrong in {name}");
					Write(e.Message);
					Write(e.StackTrace);
				}
			};
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
			Timer timer = new Timer(AddExeceptionWrapper(callback,name), null, delay, interval);
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
		public static void InvokeCommand(string command, List<string> args)
		{
			if(!commands.ContainsKey(command))
			{
				throw new NoSuchCommandException(command);
			}else
			{
				try
				{
					commands[command].Parse(args);
				}catch(Exception e)
				{
					Write($"An error occured while executing the command: {e.Message}");
					Write(e.StackTrace);
				}
			}
		}

	}
}
