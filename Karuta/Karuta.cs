using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace com.LuminousVector.Karuta
{
	public class Karuta
	{
		private bool _isRunning = false;
		private string _input;
		private string _user;
		public Karuta()
		{
			Console.Title = "Karuta";
			Console.BackgroundColor = ConsoleColor.DarkMagenta;
			Console.ForegroundColor = ConsoleColor.White;
			Say("Hello");
			_user = "user";
			Say("Karuta is ready.");
		}

		public void Run()
		{
			Console.WriteLine("Karuta is running...");
			_isRunning = true;
			while (_isRunning)
			{
				Console.Write(_user + ": ");
				_input = Console.ReadLine();
				Say(_input);
				if (_input == "stop")
				{
					Say("Goodbye");
					Close();
				}
				
			}
		}

		public void Close()
		{
			Console.WriteLine("");
			_isRunning = false;
		}

		Karuta Say(string message)
		{
			Console.WriteLine("Karuta: " + message);
			return this;
		}

	}
}
