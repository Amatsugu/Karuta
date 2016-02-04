using System;
using System.IO;
using System.Net;
using System.Xml;
//using System.Text;
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

		public int GetPositionInOptionStack(string optionStack, char option)
		{
			return optionStack.IndexOf(optionStack);
		}

		public string[] GetValueStack(string[] args, int valueStartIndex)
		{
			int size = GetValueStackSize(args, valueStartIndex);
			if (size == -1)
				return null;
			string[] valueStack = new string[size];
			for(int i = valueStartIndex; i < valueStartIndex + size; i++)
			{
				valueStack[i - valueStartIndex] = args[i];
			}
			return valueStack;
		}

		public int GetValueStackSize(string[] args, int valueStartIndex)
		{
			string optionStack = GetOptionStack(args, args[valueStartIndex - 1][1]);
			if (optionStack == null)
				return -1;
			int optionStackSize = optionStack.Length;
			if (optionStackSize == 0)
				return -1;
			int size = 0;
			for (int i = valueStartIndex; i < valueStartIndex + optionStackSize; i++)
			{
				if (args[i][0] != '-')
					size++;
				else
					break;
			}
			if (size == 0)
				return -1;
			else
				return size;
		}

		public string GetOptionStack(string[] args, char option)
		{
			string stack = null;
			foreach(string a in args)
			{
				if(a[0] == '-' && a.Contains(option))
				{
					stack = a;
					break;
				}
			}
			if(stack != null)
				stack = stack.Remove(0,1);
			return stack;
		}

		public string GetValueOfOption(string[] args, char option)
		{
			int index = GetPositionInOptionStack(GetOptionStack(args, option), option);
			if (index == -1)
				return null;
			string[] valueStack = GetValueStack(args, GetIndexOfOption(args, option));
			if (valueStack == null)
				return null;
			if (index >= valueStack.Length)
				return null;
			else
				return valueStack[index];
		}

		public int GetIndexOfOption(string[] args, char option)
		{
			for(int i = 0; i < args.Length; i++)
			{
				if (args[i][0] == '-' && args[i].Contains(option))
				{
					if (args.Length >= i)
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
			if(args.Length == 1)
			{
				Karuta.Say("The current username is: " + Karuta.user);
				return;
			}
			if(args.Length < 3)
			{
				Karuta.SayQuietly("Not enough parameters");
				Karuta.SayQuietly(usageMessage);
				return;
			}
			string user = GetValueOfOption(args, 's');
			if(user == null)
			{
				Karuta.SayQuietly("Username must be specified");
				Karuta.SayQuietly(usageMessage);
				return;
			}
			Karuta.Say("Setting username to: " + user);
			Karuta.user = user;
		}
	}
	
	//Help command
	public class HelpCommand : Command
	{
		public HelpCommand() : base("help", "show this screen", "help <commandName>") { }

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
		public List<string> validShapes;
		public DrawCommand() : base("draw", "draws ASCII shapes onto the screen", "draw -s [size:int] <square|triangle|circle>")
		{
			validShapes = new List<string>(3);
			validShapes.Add("square");
			validShapes.Add("circle");
			validShapes.Add("triangle");
		}

		public override void Run(string[] args)
		{
			if(args.Length < 4)
			{
				Karuta.SayQuietly("too few arguments");
				return;
			}
			string size = GetValueOfOption(args, 's');
			if (size == null)
			{
				Karuta.SayQuietly("no size provided");
				return;
			}
			int optionPos = GetIndexOfOption(args, 's');
			int valueStackSize = GetValueStackSize(args, optionPos);
			string shape;
			if (optionPos + valueStackSize > args.Length)
				shape = args[optionPos + valueStackSize + 1];
			else
				shape = args[optionPos - 2];
			if(shape == null)
			{
				Karuta.SayQuietly("Error");
				return;
			}
			if (validShapes.Contains(shape))
				DrawShape(shape);
			else
			{
				Karuta.SayQuietly("Invalid Shape");
				Karuta.SayQuietly(usageMessage);
			}
		}

		private void DrawShape(string shape)
		{

		}
	}

	public class PlexCommand : Command
	{
		public PlexCommand() : base("plex", "Acess music via the plex media server", "plex <action>") { }

		public override void Run(string[] args)
		{
			string serverURL = "http://karuta.luminousvector.com:32400";
			WebRequest request;
			try
			{
				request = WebRequest.Create(serverURL + "/library/metadata/2526");
				Stream response = request.GetResponse().GetResponseStream();
				StreamReader resposeReader = new StreamReader(response);
				Karuta.SayQuietly(resposeReader.ReadToEnd());
				using (XmlReader reader = XmlReader.Create(new StringReader(resposeReader.ReadToEnd())))
				{
					reader.ReadToFollowing("Track");
					reader.MoveToAttribute("title");
					string title = reader.Value;
					reader.MoveToAttribute("originalTitle");
					string artist = reader.Value;
					Karuta.SayQuietly("Now Playing:");
					Karuta.SayQuietly("\t" + artist + " - " + title);
				}
			}
			catch (Exception e)
			{
				Karuta.SayQuietly(e.Message);
			}
		}
	}

}
