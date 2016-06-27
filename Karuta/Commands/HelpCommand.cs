using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace com.LuminousVector.Karuta.Commands
{
	public class HelpCommand : Command
	{
		public HelpCommand() : base("help") { }

		private string c = null;

		protected override void Init()
		{
			base.Init();
			_default = Help;
			_helpMessage = "shows this screen.";
			RegisterOption('c', c => this.c = c);
		}

		void Help()
		{
			if (c != null)
			{
				if (Karuta.commands.ContainsKey(c))
				{
					Command c = Karuta.commands[this.c];
					Karuta.Write("\t" + c.name + "\t" + c.helpMessage);
					//if (c.usageMessage != null)
					//	Karuta.Write("\t\tUsage: " + c.usageMessage);*/
				}
				else
				{
					Karuta.Write("No such command '" + c + "'");
				}
				c = null;
			}
			else
			{
				Karuta.Write("The available commands are:");
				foreach (Command c in Karuta.commands.Values)
				{
					Karuta.Write("\t" + c.name + "\t" + c.helpMessage);
					//if (c.usageMessage != null)
					//	Karuta.Write("\t\tUsage: " + c.usageMessage);
				}
			}
		}
	}
}
