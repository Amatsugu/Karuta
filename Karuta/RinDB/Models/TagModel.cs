using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LuminousVector.Karuta.RinDB.Models
{

	public class TagModel
	{
		public TagModel(string name, string type = "tag", string description = null)
		{
			this.name = name;
			this.description = description;
			this.type = type;
		}

		public string id { get { return long.Parse($"{name.GetHashCode()}{type.GetHashCode()}").ToBas36String(); } }
		public string name { get; set; }
		public string description { get; set; }
		public string type { get; set; }
	}
}