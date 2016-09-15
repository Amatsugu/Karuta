using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LuminousVector.Karuta.Commands
{
	[KarutaCommand(Name = "help")]
	public class HelpCommand : Command
	{
		public HelpCommand() : base("help", "show this screen")
		{
			_default = Help;
			init = () =>
			{
				foreach (Command c in Karuta.commands.Values)
				{
					//Karuta.Write(c.name);
					RegisterKeyword(c.name, () =>
					{
						Karuta.Write("\t" + c.name + "\t" + c.helpMessage);
						Karuta.Write("\tOptions:");
						foreach (Option o in c.options)
							Karuta.Write("\t\t -" + o.key + "\t" + o.usage);
						Karuta.Write("\tKeywords:");
						foreach (Keyword k in c.keywords)
							Karuta.Write("\t\t " + k.keyword + "\t" + k.usage);
					});
				}
			};
			//RegisterOption('c', c => this.c = c);
		}

		private string c = null;

		void Help()
		{
			if (c != null)
			{
				if (Karuta.commands.ContainsKey(c))
				{
					Command c = Karuta.commands[this.c];
					Karuta.Write("\t" + c.name + "\t" + c.helpMessage);
					Karuta.Write("\tOptions:");
					foreach (Option o in c.options)
						Karuta.Write("\t\t -" + o.key + "\t" + o.usage);
					Karuta.Write("\tKeywords:");
					foreach (Keyword k in c.keywords)
						Karuta.Write("\t\t " + k.keyword + "\t" + k.usage);
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
