using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LuminousVector.Karuta.Commands
{
	class CommandInterpreter<T> where T:Command
	{
		public Dictionary<string, T> commands { get { return _commands; } }

		protected Dictionary<string, T> _commands;


		public CommandInterpreter()
		{
			_commands = new Dictionary<string, T>();
		}

		public CommandInterpreter<T> RegisterCommand(T command)
		{
			if (command == null)
				throw new CommandInterpretorExeception("Command cannot be null");
			if (_commands.ContainsKey(command.name))
				throw new DuplicateCommandExeception(command.name);
			_commands.Add(command.name, command);

			return this;
		}


		public virtual void Interpret(string command)
		{
			foreach (string c in command.Split('&'))
			{
				List<string> args = new List<string>();
				args.AddRange(from arg in c.Split(' ') where !string.IsNullOrWhiteSpace(arg) select arg);
				T cmd;
				_commands.TryGetValue(args[0], out cmd);
				if (cmd == null)
					throw new CommandInterpretorExeception($"No such command: \"{args[0]}\"");
				else
				{
					args.RemoveAt(0);
					cmd.Parse(args);
				}
			}
		}

		public void Init()
		{
			foreach (T c in _commands.Values)
				c.init?.Invoke();
		}
	}
}
