using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace com.LuminousVector.Karuta.Commands
{
	public class Keyword
	{
		public string keyword { get; }

		private Action _action;

		public Keyword(string keyword, Action action)
		{
			this.keyword = keyword;
			_action = action;
		}

		public void Execute()
		{
			_action();
		}

		public static bool operator ==(Keyword a, string b)
		{
			if (ReferenceEquals(a, b))
			{
				return true;
			}

			if (((object)a == null) || ((object)b == null))
			{
				return false;
			}

			return a.keyword == b;
		}

		public static bool operator !=(Keyword a, string b)
		{
			return !(a == b);
		}

		public override bool Equals(object obj)
		{
			if (obj.GetType() == typeof(string))
				return this == (string)obj;
			return base.Equals(obj);
		}

		public override int GetHashCode()
		{
			return base.GetHashCode();
		}
	}
}
