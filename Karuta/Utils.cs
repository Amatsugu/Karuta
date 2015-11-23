using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace com.LuminousVector.Karuta
{
	class Utils
	{
		public static float Clamp(float value, float range)
		{
			return Clamp(value, -range, range);
		}

		public static float Clamp(float value, float min, float max)
		{
			return (value > max) ? max : (value < min) ? min: value;
		}
	}
}
