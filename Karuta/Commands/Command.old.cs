using System;
using System.IO;
using System.Net;
using System.Xml;
using Newtonsoft.Json;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LuminousVector.Karuta.old
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

		public virtual void Close()
		{

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
				Karuta.Write("Not enough parameters");
				Karuta.Write(usageMessage);
				return;
			}
			string user = GetValueOfOption(args, 's');
			if(user == null)
			{
				Karuta.Write("Username must be specified");
				Karuta.Write(usageMessage);
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
				Karuta.Write("The available commands are:");
				/*foreach(Command c in Karuta.commands.Values)
				{
					Karuta.Write("\t" + c.name + "\t" + c.helpMessage);
					if (c.usageMessage != null)
						Karuta.Write("\t\tUsage: " + c.usageMessage);
				}*/
			}else
			{
				for(int i = 1; i < args.Length; i++)
				{
					if(Karuta.commands.ContainsKey(args[i]))
					{
						/*Command c = Karuta.commands[args[i]];
						Karuta.Write("\t" + c.name + "\t" + c.helpMessage);
						if (c.usageMessage != null)
							Karuta.Write("\t\tUsage: " + c.usageMessage);*/
					}else
					{
						Karuta.Write("No such command '" + args[i] + "'");
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
				Karuta.Write("too few arguments");
				return;
			}
			string size = GetValueOfOption(args, 's');
			if (size == null)
			{
				Karuta.Write("no size provided");
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
				Karuta.Write("Error");
				return;
			}
			if (validShapes.Contains(shape))
				DrawShape(shape);
			else
			{
				Karuta.Write("Invalid Shape");
				Karuta.Write(usageMessage);
			}
		}

		private void DrawShape(string shape)
		{

		}
	}

	public class PlexCommand : Command
	{
		public PlexCommand() : base("plex", "Acess music via the plex media server", "plex <action>") { }
		public string serverURL = "http://karuta.luminousvector.com:32400";

		private string _authToken;
		//private StreamReader responseReader;

		public override void Run(string[] args)
		{
			WebRequest request;
			if (args.Length == 3)
			{
				string req = GetValueOfOption(args, 'r');
				if (req == null)
				{
					Karuta.Write("A request parameter must be provided");
					return;
				}
				try
				{
					request = WebRequest.Create(serverURL + req);
					Stream response = request.GetResponse().GetResponseStream();
					using (StreamReader responseReader = new StreamReader(response))
						Karuta.Write(responseReader.ReadToEnd());
				} catch (Exception e)
				{
					Karuta.Write(e.Message);
				}
				return;
			}
			try
			{
				request = WebRequest.Create(serverURL + "/status/sessions");
				WebHeaderCollection header = new WebHeaderCollection();
				if (_authToken == null)
					_authToken = GetAuthToken();
				Karuta.Write("Auth: " + _authToken);
				header.Add("X-Plex-Token", _authToken);
				request.Headers = header;
				using (Stream response = request.GetResponse().GetResponseStream())
				{
					using (StreamReader responseReader = new StreamReader(response))
					{
						Karuta.Write(responseReader.ReadToEnd());
						using (XmlReader reader = XmlReader.Create(new StringReader(responseReader.ReadToEnd())))
						{
							reader.ReadToFollowing("Track");
							reader.MoveToAttribute("title");
							string title = reader.Value;
							reader.MoveToAttribute("originalTitle");
							string artist = reader.Value;
							Karuta.Write("Now Playing:");
							Karuta.Write("\t" + artist + " - " + title);
						}
					}
				}
			}
			catch (Exception e)
			{
				Karuta.Write(e.Message);
			}
			Close();
		}

		public override void Close()
		{
			_cookies = null;
		}

		private CookieCollection _cookies;
		
		private void GetCookies()
		{
			_cookies = new CookieCollection();
			HttpWebRequest request = (HttpWebRequest)WebRequest.Create("https://plex.tv");
			request.CookieContainer = new CookieContainer();
			request.CookieContainer.Add(_cookies);
			//Get the response from the server and save the cookies from the first request..
			HttpWebResponse response = (HttpWebResponse)request.GetResponse();
			_cookies = response.Cookies;
			response.Close();
		}


		private string GetAuthToken()
		{
			if (_cookies == null)
				GetCookies();
			string pass = Karuta.GetInput("Please enter your Plex password", true);
			string postData = String.Format("user[login]={0}&user[password]={1}", Karuta.user, pass);
			HttpWebRequest getRequest = (HttpWebRequest)WebRequest.Create("https://my.plexapp.com/users/sign_in.xml");
			getRequest.CookieContainer = new CookieContainer();
			getRequest.CookieContainer.Add(_cookies); //recover cookies First request
			getRequest.Method = WebRequestMethods.Http.Post;
			getRequest.UserAgent = "Mozilla/5.0 (Windows NT 6.1) AppleWebKit/535.2 (KHTML, like Gecko) Chrome/15.0.874.121 Safari/535.2";
			getRequest.AllowWriteStreamBuffering = true;
			getRequest.ProtocolVersion = HttpVersion.Version11;
			getRequest.AllowAutoRedirect = true;
			getRequest.ContentType = "application/x-www-form-urlencoded";

			byte[] byteArray = Encoding.ASCII.GetBytes(postData);
			getRequest.ContentLength = byteArray.Length;
			Stream newStream = getRequest.GetRequestStream(); //open connection
			newStream.Write(byteArray, 0, byteArray.Length); // Send the data.
			newStream.Close();
			HttpWebResponse response = (HttpWebResponse)getRequest.GetResponse();
			//Retrieve your cookie that id's your session
			//response.Cookies
			StreamReader reader = new StreamReader(response.GetResponseStream());
			string output = reader.ReadToEnd();
			reader.Close();
			reader.Dispose();
			response.Close();
			return output;
		}
	}

}
