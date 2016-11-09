using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;

namespace LuminousVector.Karuta.Commands
{
	/// <summary>
	/// Basic command
	/// </summary>
	/// <param name="name">The name of this command, used to execute the command</param>
	/// <param name="defaultAction">The default action performed when this command is executed with no keywords</param>
	/// <param name="helpMessage"/> The help message for this command, shown by the help command</param>
	public class Command : ICommand
	{
		public string name { get; }
		public string helpMessage { get { return _helpMessage; } }
		public List<Option> options { get { return (from o in _options select o.Value).ToList(); } }
		public List<Keyword> keywords { get { return (from k in _keywords select k.Value).ToList(); } }
		public Action init;

		protected Action _default { get; set; }
		protected string _helpMessage;

		private Dictionary<char, Option> _options;
		private Dictionary<string, Keyword> _keywords;

		public Command(string name, string helpMessage) : this(name, null, helpMessage)
		{
			
		}

		public Command(string name, Action defaultAction, string helpMessage = null)
		{
			this.name = name;
			_default = defaultAction;
			_helpMessage = helpMessage;
			_options = new Dictionary<char, Option>();
			_keywords = new Dictionary<string, Keyword>();
		}

		protected ICommand RegisterKeyword(string keyword, Action action, string usage = "")
		{
			_keywords.Add(keyword, new Keyword(keyword, action, usage));
			return this;
		}

		protected ICommand RegisterOption(char key, Action action, string usage = "")
		{
			_options.Add(key, new Option(key, action, usage));
			return this;
		}

		protected ICommand RegisterOption(char key, Action<string> action, string usage = "")
		{
			_options.Add(key, new Option(key, action, usage));
			return this;
		}

		protected void ClearKeywords()
		{
			_keywords.Clear();
		}

		protected void ClearOptions()
		{
			_options.Clear();
		}

		public virtual ICommand Parse(List<string> args)
		{
			//Find keyword to execute
			Keyword selectedKeyword = null;

			if((_keywords.Count == 0 && _options.Count == 0) || args.Count == 0)
			{
				return Execute();
			}

			Debug.Print("Raw Args");
			Debug.Print( string.Join(" ", from s in args select s));
			Debug.Print("Keyword parse START");
			if (_keywords.Count != 0)
			{
				//Select and assign the keyword if it exists
				IList<Keyword> kl = (from arg in args where _keywords.ContainsKey(arg) select _keywords[arg]).ToList();
				selectedKeyword = (kl.Count == 0) ? null : kl.First();
				if (selectedKeyword != null)
					args.Remove(selectedKeyword.keyword);
			}
			Debug.Print("Keyword parse END");
			Debug.Print($">{string.Join(", ", from s in args select s)}");
			//Find options to set
			if (args.Count != 0)
			{
				IList<string> emptyArgs = (from empty in args where string.IsNullOrWhiteSpace(empty) select empty).ToList();

				foreach (string arg in emptyArgs)
					args.Remove(arg);

				//Replace single quotes with double quotes if they exist
				Debug.Print("Quote balance check START");
				for (int i = 0; i < args.Count; i++)
				{
					args[i] = args[i].Replace("'", "\"");
					if (args[i][0] == '\"' && args[i][args[i].Length - 1] == '\"')
						args[i] = args[i].Replace("\"", "");
				}

				if (!Utils.IsEven((from q in args where q.Contains("\"") select q).ToList().Count))
					throw new CommandParsingExeception("Unbalaced Quotes detected");
				Debug.Print("Quote balance check END");
				Debug.Print($">{string.Join(", ", from s in args select s)}");

				string optKeys = "";

				Debug.Print("Option flags parse START");
				//Find options flags
				optKeys = string.Join("", from o in args where o.Contains("-") select o.Remove(0, 1));

				List<string> removalQ = new List<string>();
				removalQ.AddRange(from a in args where a.Contains("-") select a);

				foreach (string a in removalQ)
					args.Remove(a);
				Debug.Print("Option flags parse END");
				Debug.Print($">{string.Join(", ", from s in args select s)}");

				Debug.Print("Quote Parse START");
				List<int> start = new List<int>(), size = new List<int>();
				//Find location and size of quotes
				for(int i = 0; i < args.Count; i++)
				{
					if (args[i].Contains("\""))
					{
						if (start.Count == size.Count)
							start.Add(i);
						else
							size.Add(i - start.Last() + 1);
					}
				}

				//Find and merge quoted text
				int offset = 0;
				foreach (int s in start)
				{
					int i = start.IndexOf(s);
					string quote = string.Join(" ", args.GetRange(s - offset, size[i])).Replace("\"", "");
					args.RemoveRange(s - offset, size[i]);
					args.Insert(s - offset, quote);
					offset += size[i] - 1;
				}

				Debug.Print("Quote Parse END");
				Debug.Print($">{string.Join(", ", from s in args select s)}");

				int index = 0;
				Debug.Print("Option parse START");
				//Execute each option and pass pararemter if needed
				foreach (char key in optKeys)
				{
					Option opt;
					if(!_options.TryGetValue(key, out opt))
						throw new CommandParsingExeception($"No such option -{key.ToString()} {optKeys}");
					if (opt.isParamLess)
						opt.Execute();
					else
					{
						if (args.Count <= index)
							Karuta.Write("No value provided for option: " + opt.key);
						else
							opt.Execute(args[index++]);
					}
				}
				Debug.Print("Option parse END");
				Debug.Print($">{string.Join(", ", from s in args select s)}");
			}
			Debug.Print("EXECUTE");
			//Execute keyword if found
			if (selectedKeyword != null)
			{
				selectedKeyword.Execute();
				return this;
			}
			//Execute default
			return Execute();
		}

		public ICommand Execute()
		{
			_default?.Invoke();
			return this;
		}

		public virtual void Stop()
		{
		}
	}
}
