using System;
using System.Collections.Generic;
using System.Speech.Synthesis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;

namespace com.LuminousVector.Karuta
{
	public static class Karuta
	{
		private static bool _isRunning = false;
		private static string _input;
		private static string _user;
		private static SpeechSynthesizer _voice;
		private static Dictionary<string,Command> commands = new Dictionary<string,Command>();
		static Karuta()
		{
			Console.Title = "Karuta";
			Console.BackgroundColor = ConsoleColor.DarkMagenta;
			Console.ForegroundColor = ConsoleColor.White;
			Console.Clear();
			Stopwatch sw = new Stopwatch();
			sw.Start();
			SayQuietly("Preparing Karuta...");
			_voice = new SpeechSynthesizer();
			_voice.SelectVoiceByHints(VoiceGender.Female, VoiceAge.Teen);
			_user = "user";
			RegisterCommand(new Command("stop", Close));
			RegisterCommand(new Command("clear", Console.Clear));
			RegisterCommand(new UserCommand());
			RegisterCommand(new MakeCommand());
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
				Console.Write(_user + ": ");
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

		public static void SetUser(string user)
		{
			Say("Setting user to: " + user);
			_user = user;
		}

		public static void Close()
		{
			_voice.Dispose();
			Console.WriteLine("GoodBye");
			_isRunning = false;
		}

		public static void Say(string message)
		{
			_voice.SpeakAsyncCancelAll();
			_voice.SpeakAsync(message);
			Console.WriteLine("Karuta: " + message);
		}

		public static void SayQuietly(string message)
		{
			Console.WriteLine(message);
		}

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
