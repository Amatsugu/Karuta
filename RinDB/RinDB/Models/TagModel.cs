using System;
using LuminousVector.Utils.Extensions;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LuminousVector.RinDB.Models
{

	public class TagModel
	{
		public TagModel(string name, string type = "tag", string description = "")
		{
			this.name = Uri.UnescapeDataString(name);
			if(!string.IsNullOrWhiteSpace(description))
				this.description = Uri.UnescapeDataString(description);
			this.type = Uri.UnescapeDataString(type);
		}

		public string id { get { return name.ToBase60(); } }
		public string name { get; set; }
		public string description { get; set; }
		public string type { get; set; }
		public string parentID { get; set; }
	}
}
