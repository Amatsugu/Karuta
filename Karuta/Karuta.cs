using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace com.LuminousVector.Karuta
{
	public static class Karuta
	{
		private static bool _isRunning = false;
		private static string _input;
		private static string _user;
		private static Dictionary<string,Command> commands = new Dictionary<string,Command>();
		static Karuta()
		{
			Console.Title = "Karuta";
			Console.BackgroundColor = ConsoleColor.DarkMagenta;
			Console.ForegroundColor = ConsoleColor.White;
			Console.Clear();
			Say("Hello");
			_user = "user";
			RegisterCommand(new Command("stop", Close));
			RegisterCommand(new Command("clear", Console.Clear));
			RegisterCommand(new UserSetCommand());
			Say("Karuta is ready.");
		}

		public static void RegisterCommand(Command command)
		{
			commands.Add(command.name, command);
		}

		public static void Run()
		{
			Console.WriteLine("Karuta is running...");
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
			Console.WriteLine("GoodBye");
			_isRunning = false;
		}

		public static void Say(string message)
		{
			Console.WriteLine("Karuta: " + message);
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
