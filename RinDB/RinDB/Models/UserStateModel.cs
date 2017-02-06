using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LuminousVector.RinDB.Models
{
	public class UserStateModel
	{
		public static UserStateModel DEFAULT = new UserStateModel("GUEST")
		{
			userName = "guest",
			profile = "res/img/DefaultThumb.png",
			night = false
		};

		public string userID { get; }
		public string userName { get; set; }
		public string profile { get; set; }
		public bool night { get; set; }

		public UserStateModel(string ID)
		{
			userID = ID;
		}

	}
}
