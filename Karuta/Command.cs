using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace com.LuminousVector.Karuta
{
	public class Command
	{
		public string name { get { return _name; } }
		public string helpMessage { get { return _helpMessage; } }
		private string _name;
		private string _helpMessage;
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

		public Command(string name, string helpMessage, Action action)
		{
			_name = name;
			_helpMessage = helpMessage;
			_action = action;
		}

		public virtual void Run(string[] args)
		{
			_action.Invoke();
		}
	}

	public class UserSetCommand : Command
	{
		public UserSetCommand() : base("user-set", "user-set [username]") { }

		public override void Run(string[] args)
		{
			if (args.Length <= 1)
			{
				Karuta.Say("A user must be specified");
				Karuta.Say(helpMessage);
			}
			else
			{
				Karuta.SetUser(args[1]);
			}
		}
	}
}
