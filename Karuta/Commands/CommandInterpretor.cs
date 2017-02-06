using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LuminousVector.Utils.Extensions;

namespace LuminousVector.Karuta.Commands
{
	class CommandInterpreter<T> where T:Command
	{
		public List<CommandIdentity> commandIDs
		{
			get
			{
				List<CommandIdentity> cIDs = new List<CommandIdentity>();
				cIDs.AddRange(from Command c in _commands.Values select c.identity);
				cIDs.AddRange(from dynamic c in _dynamicCommands.Values select (CommandIdentity)c.identity);
				return cIDs;
			}
		}

		protected Dictionary<string, T> _commands;
		protected Dictionary<string, dynamic> _dynamicCommands;


		public CommandInterpreter()
		{
			_commands = new Dictionary<string, T>();
			_dynamicCommands = new Dictionary<string, dynamic>();
		}

		public CommandInterpreter<T> RegisterDynamicCommand(dynamic command)
		{
			if (command == null)
				throw new CommandInterpreterExeception("Command cannot be null");
			if (_commands.ContainsKey(command.name) || _dynamicCommands.ContainsKey(command.name))
				throw new DuplicateCommandExeception(command.name);
			_dynamicCommands.Add(command.name, command);
			return this;
		}

		public CommandInterpreter<T> RegisterCommand(T command)
		{
			if (command == null)
				throw new CommandInterpreterExeception("Command cannot be null");
			if (_commands.ContainsKey(command.name) || _dynamicCommands.ContainsKey(command.name))
				throw new DuplicateCommandExeception(command.name);
			_commands.Add(command.name, command);

			return this;
		}

		public List<T> GetCommands()
		{
			return _commands.Values.ToList();
		}

		public T GetCommand(string command)
		{
			T cmd;
			_commands.TryGetValue(command, out cmd);
			return cmd;
		}

		public void Clear()
		{
			_commands.Clear();
			_dynamicCommands.Clear();
		}

		public F GetCommandOfType<F>()
		{
			return (from F c in _commands where c.GetType() == typeof(F) select c).First();
		}

		public bool CommandExists(string command)
		{
			if (_commands.ContainsKey(command))
				return true;
			else if (_dynamicCommands.ContainsKey(command))
				return true;
			else
				return false;
		}

		public bool RemoveCommand(string command)
		{
			if(_commands.ContainsKey(command))
			{
				_commands.Remove(command);
				return true;
			}else if(_dynamicCommands.ContainsKey(command))
			{
				_dynamicCommands.Remove(command);
				return true;
			}
			return false;
		}

		public bool SetHelpMessage(string command, string helpMessage)
		{
			if (_commands.ContainsKey(command))
			{
				_commands[command].identity.SetHelpMessage(helpMessage);
				return true;
			}
			else if (_dynamicCommands.ContainsKey(command))
			{
				_dynamicCommands[command].identity.SetHelpMessage(helpMessage);
				return true;
			}
			else
				return false;
		}

		public virtual void Interpret(string command)
		{
			foreach (string c in command.Split('&'))
			{
				List<string> args = c.SplitPreserveGrouping();
				string cmd = args[0];
				args.RemoveAt(0);
				Invoke(cmd, args);
			}
		}

		public void Init()
		{
			foreach (T c in _commands.Values)
				c.init?.Invoke();
			foreach (dynamic c in _dynamicCommands.Values)
				c.init?.Invoke();
		}

		public void Stop()
		{
			foreach (T c in _commands.Values)
				c.Stop();
			foreach (dynamic c in _dynamicCommands.Values)
				c.Stop();
		}

		public virtual void Invoke(string command, List<string> args)
		{
			T cmd;
			_commands.TryGetValue(command, out cmd);
			if (cmd == null)
			{
				dynamic dCmd;
				_dynamicCommands.TryGetValue(command, out dCmd);
				if (dCmd == null)
					throw new CommandInterpreterExeception($"No such command: \"{command}\"");
				else
				{
					dCmd.Parse(args);
				}
			}
			else
			{
				cmd.Parse(args);
			}
		}
	}
}
