using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace com.LuminousVector.Karuta.Commands
{
	class UserCommand : Command
	{
		public UserCommand() : base("user", "Allows you to change the username.")
		{
			RegisterOption('s', s =>
			{
				Karuta.Say("Setting username to:" + s);
				Karuta.user = s;
			});
		}
	}
}
