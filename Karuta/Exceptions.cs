using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace com.LuminousVector.Karuta
{

	public class NoSuchTimerExeception : Exception
	{
		public NoSuchTimerExeception(string name) : base("Timer: '" + name + "' does not exsist") { }
	}

	public class NoSuchCommandException : Exception
	{
		public NoSuchCommandException(string name) : base("Command: '" + name + "' does not exist") { }
	}

	public class DuplicateTimerExeception : Exception
	{
		public DuplicateTimerExeception(string name) : base("A timer with the name: '" + name + "' already exists") { }
	}

	public class DuplicateCommandExeception : Exception
	{
		public DuplicateCommandExeception(string name) : base("A command with the name: '" + name + "' has already been registed") { }
	}
}
