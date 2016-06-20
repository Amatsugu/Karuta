using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace com.LuminousVector.Karuta.Commands
{
	public class Option
	{
		public char key { get; }
		public bool isParamLess { get; } = false;

		private Action<string> _action;
		private Action _noParAction;

		public Option(char key, Action<string> action)
		{
			this.key = key;
			_action = action;
		}

		public Option(char key, Action action)
		{
			this.key = key;
			isParamLess = true;
			_noParAction = action;
		}

		public void Execute(string arg)
		{
			_action?.Invoke(arg);
		}

		public void Execute()
		{
			_noParAction?.Invoke();
		}

		public static bool operator ==(Option a, char b)
		{
			return a.key == b;
		}

		public static bool operator !=(Option a, char b)
		{
			return a.key != b;
		}

		public override bool Equals(object obj)
		{
			if (obj.GetType() == typeof(char))
				return this == (char)obj;
			return base.Equals(obj);
		}

		public override int GetHashCode()
		{
			return base.GetHashCode();
		}
	}
}
