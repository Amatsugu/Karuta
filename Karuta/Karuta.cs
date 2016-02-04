using System;
using System.IO;
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
		private static Dictionary<string,Command> _commands = new Dictionary<string,Command>();
		static Karuta()
		{
			//Prepare Console
			Console.Title = "Karuta";
			Console.BackgroundColor = ConsoleColor.DarkMagenta;
			Console.ForegroundColor = ConsoleColor.White;
			Console.Clear();
			Stopwatch sw = new Stopwatch();
			sw.Start();
			SayQuietly("Preparing Karuta...");
			//Init Event Manager
			eventManager = new EventManager();
			eventManager.Init();
			//Init Registry
			if(File.Exists("karuta.data"))
			{
				registry = DataSerializer.deserializeData<Registry>(File.ReadAllBytes("karuta.data"));
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
			sw.Stop();
			long elapsedT = sw.ElapsedTicks / (Stopwatch.Frequency / (1000L));
			SayQuietly("Karuta is ready. Finished in " + elapsedT + "ms");
		}

		public static void RegisterCommand(Command command)
		{
			commands.Add(command.name, command);
		}

		public static void Run()
		{
			SayQuietly("Karuta is running...");
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
			File.WriteAllBytes("karuta.data", DataSerializer.serializeData<Registry>(registry));
			_voice.Dispose();
			Console.WriteLine("GoodBye");
			_isRunning = false;
		}

		//Say message with voice and name label
		public static void Say(string message)
		{
			_voice.SpeakAsyncCancelAll();
			_voice.SpeakAsync(message);
			Console.WriteLine("Karuta: " + message);
		}

		//Say a message without name label or voice
		public static void SayQuietly(string message)
		{
			Console.WriteLine(message);
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
