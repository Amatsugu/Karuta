using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace com.LuminousVector.Karuta
{
	//Base command
	public class Command
	{
		public string name { get { return _name; } }
		public string helpMessage { get { return _helpMessage; } }
		public string usageMessage { get { return _usageMessage; } }
		private string _name;
		private string _helpMessage;
		private string _usageMessage;
		protected Action _action;

		public Command(string name, Action action)
		{
			_name = name;
			_action = action;
		}

		public Command(string name, string helpMessage)
		{
			_name = name;
			_helpMessage = helpMessage;
		}

		public Command(string name, string helpMessage, string usageMessage)
		{
			_name = name;
			_helpMessage = helpMessage;
			_usageMessage = usageMessage;
		}

		public Command(string name, string helpMessage, Action action)
		{
			_name = name;
			_helpMessage = helpMessage;
			_action = action;
		}

		public Command(string name, string helpMessage, string usageMessage, Action action)
		{
			_name = name;
			_helpMessage = helpMessage;
			_usageMessage = usageMessage;
			_action = action;
		}

		public virtual void Run(string[] args)
		{
			_action.Invoke();
		}

		public string GetOptions(string[] args)
		{
			string options = "";
			foreach(string a in args)
			{
				if(a[0] == '-')
				{
					options += a.Remove(0, 1);
				}
			}
			return options;
		}

		public int GetIndexOfOption(string[] args, char option)
		{
			for(int i = 0; i < args.Length; i++)
			{
				if (args[i].Contains('-') && args[i].Contains(option))
				{
					if (args.Length - 1 >= i + 1)
						return i + 1;
					else
						return -1;
				}
			}
			return -1;
		}
	}

	//User control command
	public class UserCommand : Command
	{
		public UserCommand() : base("user", "Modify the current user.", "user -s [username:string]") { }

		public override void Run(string[] args)
		{
			if (args.Length < 2)
			{
				Karuta.SayQuietly("Not enough parameters");
				Karuta.SayQuietly(usageMessage);
			}
			else
			{
				string opt = GetOptions(args);
                if (opt.Length == 1)
				{
					if(opt == "s")
					{
						int index = GetIndexOfOption(args,'s');
						if (index != -1)
						{
							Karuta.user = args[index];
						}
						else
						{
							Karuta.SayQuietly("A user must be specified");
							Karuta.SayQuietly(usageMessage);
						}
					}else
					{
						Karuta.SayQuietly("Invalid option: '" + opt.Remove(opt.IndexOf('s'),1) + "'");
						Karuta.SayQuietly(usageMessage);
					}
				}else
				{
					if (opt.Length != 0)
					{
						Karuta.SayQuietly("Extra options: '" + opt.Remove(opt.IndexOf('s'), 1) + "'");
						Karuta.SayQuietly(usageMessage);
					}else
					{
						Karuta.SayQuietly("Invalid command format.");
						Karuta.SayQuietly(usageMessage);
					}
				}
			}
		}
	}
	
	//Help command
	public class HelpCommand : Command
	{
		public HelpCommand() : base("help", "show this screen") { }

		public override void Run(string[] args)
		{
			if(args.Length == 1)
			{
				Karuta.SayQuietly("The available commands are:");
				foreach(Command c in Karuta.commands.Values)
				{
					Karuta.SayQuietly("\t" + c.name + "\t" + c.helpMessage);
					if (c.usageMessage != null)
						Karuta.SayQuietly("\t\tUsage: " + c.usageMessage);
				}
			}else
			{
				for(int i = 1; i < args.Length; i++)
				{
					if(Karuta.commands.ContainsKey(args[i]))
					{
						Command c = Karuta.commands[args[i]];
						Karuta.SayQuietly("\t" + c.name + "\t" + c.helpMessage);
						if (c.usageMessage != null)
							Karuta.SayQuietly("\t\tUsage: " + c.usageMessage);
					}else
					{
						Karuta.SayQuietly("No such command '" + args[i] + "'");
					}
				}
			}
		}
	}

	public class DrawCommand : Command
	{
		public DrawCommand() : base("draw", "draws ASCII shapes onto the screen", "draw -s [size:int] <shape>") { }

		public override void Run(string[] args)
		{
			if(args.Length > 1)
			{
				string opt = GetOptions(args);
				if(opt.Length == 1)
				{
					int index = GetIndexOfOption(args, 's');
					if (index != -1)
					{
						int size;
						if(int.TryParse(args[index], out size))
						{
							if(index == args.Length -1)
							{
								DrawShape(args[1]);
							}else if(index == 2)
							{
								DrawShape(args[index + 1]);
							}else
							{
								Karuta.SayQuietly("You must specify a shape to draw.");
								Karuta.SayQuietly("/tAbailable shapes are: square, triangle, circle");
								Karuta.SayQuietly(usageMessage);
							}
						}else
						{
							Karuta.SayQuietly("Unable to parse size: '" + args[index] + "'");
							Karuta.SayQuietly(usageMessage);
						}
					}
					else
					{
						Karuta.SayQuietly("Invalid options(s): '" + (opt.Contains('s') ? opt.Remove(opt.IndexOf('s'), 1) : opt) + "'");
						Karuta.SayQuietly(usageMessage);
					}
				}
				else
				{
					if (opt.Length != 0)
					{
						Karuta.SayQuietly("Invalid option(s): '" + opt.Remove(opt.IndexOf('s'), 1) + "'");
						Karuta.SayQuietly(usageMessage);
					}else
					{
						Karuta.SayQuietly("A size must be specified");
						Karuta.SayQuietly(usageMessage);
					}
				}
			}else
			{
				Karuta.SayQuietly("You must specify the shape to draw and the size");
				Karuta.SayQuietly(usageMessage);
			}
		}

		private void DrawShape(string shape)
		{

		}
	}

}
