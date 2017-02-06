using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LuminousVector.Karuta
{
	[Serializable]
	public class NoSuchTimerExeception : Exception
	{
		public NoSuchTimerExeception(string name) : base("Timer: '" + name + "' does not exsist") { }
	}

	[Serializable]
	public class NoSuchCommandException : CommandInterpreterExeception
	{
		public NoSuchCommandException(string name) : base("Command: '" + name + "' does not exist") { }
	}

	[Serializable]
	public class DuplicateTimerExeception : CommandInterpreterExeception
	{
		public DuplicateTimerExeception(string name) : base("A timer with the name: '" + name + "' already exists") { }
	}

	[Serializable]
	public class DuplicateCommandExeception : CommandInterpreterExeception
	{
		public DuplicateCommandExeception(string name) : base("A command with the name: '" + name + "' has already been registed") { }
	}

	[Serializable]
	public class CommandParsingExeception : Exception
	{
		public CommandParsingExeception(string message) : base(message)
		{

		}
	}

	[Serializable]
	public class DiscordEventExeception : Exception
	{
		public DiscordEventExeception(string message) : base(message)
		{

		}
	}

	[Serializable]
	public class CommandInterpreterExeception : Exception
	{
		public CommandInterpreterExeception(string message) : base(message)
		{

		}
	}
}
