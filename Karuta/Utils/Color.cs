using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LuminousVector.Karuta.Util
{
	public class Color
	{
		public int h;
		public int s;
		public int b;

		public Color()
		{
			h = s = b = 0;
		}

		public Color(string colorString)
		{
			string[] channels = colorString.Split(':');
			int.TryParse(channels[0], out h);
			int.TryParse(channels[1], out s);
			int.TryParse(channels[2], out b);
		}

		public Color(int h, int s, int b)
		{
			this.h = h;
			this.s = s;
			this.b = b;
		}

		public override string ToString()
		{
			return h + ":" + s + ":" + b;
		}
	}
}
