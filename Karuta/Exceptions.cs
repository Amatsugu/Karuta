using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LuminousVector.Karuta
{

	public class NoSuchTimerExeception : Exception
	{
		public NoSuchTimerExeception(string name) : base("Timer: '" + name + "' does not exsist") { }
	}

	public class NoSuchCommandException : CommandInterpretorExeception
	{
		public NoSuchCommandException(string name) : base("Command: '" + name + "' does not exist") { }
	}

	public class DuplicateTimerExeception : CommandInterpretorExeception
	{
		public DuplicateTimerExeception(string name) : base("A timer with the name: '" + name + "' already exists") { }
	}

	public class DuplicateCommandExeception : CommandInterpretorExeception
	{
		public DuplicateCommandExeception(string name) : base("A command with the name: '" + name + "' has already been registed") { }
	}

	public class CommandParsingExeception : Exception
	{
		public CommandParsingExeception(string message) : base(message)
		{

		}
	}

	public class DiscordEventExeception : Exception
	{
		public DiscordEventExeception(string message) : base(message)
		{

		}
	}

	public class CommandInterpretorExeception : Exception
	{
		public CommandInterpretorExeception(string message) : base(message)
		{

		}
	}
}
