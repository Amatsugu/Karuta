using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LuminousVector.Karuta.Commands
{
	public class CommandIdentity
	{

		public string name { get; set; }
		public string helpMessage { get; set; }
		public Type type { get; }

		public List<Keyword> keywords { get { return _keywords; } }
		public List<Option> options { get { return _options; } }

		public List<Keyword> _keywords;
		public List<Option> _options;

		public CommandIdentity(string name) : this(name, "", typeof(Command))
		{
		}

		public CommandIdentity(string name, string helpMessage, Type type)
		{
			this.name = name;
			this.helpMessage = helpMessage;
			this.type = type;
		}

		public void SetOptionsAndKeywords(List<Keyword> keywords, List<Option> options)
		{
			_keywords = keywords;
			_options = options;
		}

		public CommandIdentity SetHelpMessage(string helpMessage)
		{
			this.helpMessage = helpMessage;
			return this;
		}

		public static bool operator ==(CommandIdentity a, string b)
		{
			if (ReferenceEquals(a, b))
			{
				return true;
			}

			if (((object)a == null) || ((object)b == null))
			{
				return false;
			}

			return a.name == b;
		}

		public static bool operator !=(CommandIdentity a, string b)
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
			return name.GetHashCode();
		}
	}
}
