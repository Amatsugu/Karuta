using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace com.LuminousVector.Karuta
{
	public class NoSuchCommandException : Exception
	{
		public NoSuchCommandException() : base("No such command") { }
	}
}
