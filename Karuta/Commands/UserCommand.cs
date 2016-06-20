using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace com.LuminousVector.Karuta.Commands
{
	class UserCommand : Command
	{
		public UserCommand() : base("user") { }

		protected override void Init()
		{
			base.Init();
			_helpMessage = "Allows you to change the username.";
			RegisterOption('s', ChangeUser);
		}

		void ChangeUser(string user)
		{
			Karuta.Say("Setting username to: " + user);
			Karuta.user = user;
		}
	}
}