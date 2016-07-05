using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace com.LuminousVector.Karuta.Commands
{
	public class Command : ICommand
	{
		public string name { get; }
		public string helpMessage { get { return _helpMessage; } }
		public ReadOnlyCollection<Option> options { get { return _options.AsReadOnly(); } }
		public ReadOnlyCollection<Keyword> keywords { get { return _keywords.AsReadOnly(); } }

		protected Action _default { get; set; }
		protected string _helpMessage;

		private List<Option> _options;
		private List<Keyword> _keywords;

		public Command(string name, string helpMessage) : this(name, null, helpMessage) { }

		public Command(string name, Action defaultAction, string helpMessage)
		{
			this.name = name;
			_default = defaultAction;
			_helpMessage = helpMessage;
			_options = new List<Option>();
			_keywords = new List<Keyword>();
		}

		protected ICommand RegisterKeyword(string keyword, Action action)
		{
			return RegisterKeyword(keyword, action, "");
		}

		protected ICommand RegisterKeyword(string keyword, Action action, string usage)
		{
			_keywords.Add(new Keyword(keyword, action, usage));
			return this;
		}

		protected ICommand RegisterOption(char key, Action action)
		{
			return RegisterOption(key, action, "");
		}

		protected ICommand RegisterOption(char key, Action action, string usage)
		{
			_options.Add(new Option(key, action, usage));
			return this;
		}

		protected ICommand RegisterOption(char key, Action<string> action)
		{
			return RegisterOption(key, action, "");
		}

		protected ICommand RegisterOption(char key, Action<string> action, string usage)
		{
			_options.Add(new Option(key, action, usage));
			return this;
		}

		public ICommand Pharse(List<string> args)
		{
			//Find keyword to execute
			Keyword selectedKeyword = null;
			if((_keywords.Count == 0 && _options.Count == 0) || args.Count == 0)
			{
				return Execute();
			}

			if (_keywords.Count != 0)
			{

				foreach (Keyword keyword in _keywords)
				{
					foreach (string arg in args)
					{
						if (keyword == arg)
						{
							selectedKeyword = keyword;
							break;
						}
					}
					if (selectedKeyword != null)
						break;
				}
				if (selectedKeyword != null)
					args.Remove(selectedKeyword.keyword);
			}


			//Find options to set
			if (args.Count != 0)
			{
				string optKeys = "";
				List<string> removalQ = new List<string>();

				foreach(string arg in args)
				{
					if (arg.Contains("-"))
					{
						optKeys += arg.Remove(0, 1);
						removalQ.Add(arg);
					}
				}

				foreach (string a in removalQ)
					args.Remove(a);
				removalQ.Clear();

				int index = 0;
				foreach (char key in optKeys)
				{
					foreach (Option opt in _options)
					{	
						if(opt == key)
						{
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
					}
				}
			}
			if (selectedKeyword != null)
			{
				selectedKeyword?.Execute();//Execute keyword if found
				return this;
			}
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
