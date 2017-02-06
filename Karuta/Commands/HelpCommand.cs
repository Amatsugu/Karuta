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
				foreach (CommandIdentity c in Karuta.commandIDs)
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
				CommandIdentity cmd = (from CommandIdentity cID in Karuta.commandIDs where cID.name == c select cID).First();
				if (cmd != null)
				{
					Karuta.Write("\t" + cmd.name + "\t" + cmd.helpMessage);
					Karuta.Write("\tOptions:");
					foreach (Option o in cmd.options)
						Karuta.Write("\t\t -" + o.key + "\t" + o.usage);
					Karuta.Write("\tKeywords:");
					foreach (Keyword k in cmd.keywords)
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
				foreach (CommandIdentity c in Karuta.commandIDs)
				{
					Karuta.Write("\t" + c.name + "\t" + c.helpMessage);
					//if (c.usageMessage != null)
					//	Karuta.Write("\t\tUsage: " + c.usageMessage);
				}
			}
		}
	}
}
