using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LuminousVector.RinDB.Models
{
	public class WidgetModel
	{
		public string name { get; set; }
		public object value { get; set; }

		public WidgetModel(string name, object value)
		{
			this.name = name;
			this.value = value;
		}
	}
}
