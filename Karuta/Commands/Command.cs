using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace com.LuminousVector.Karuta.Commands
{
	public class Command : ICommand
	{
		public string name { get; }
		public string helpMessage { get { return _helpMessage; } }

		protected Action _default { get; set; }
		protected string _helpMessage;

		private List<Option> _options;
		private List<Keyword> _keywords;

		public Command(string name)
		{
			this.name = name;
			Init();
		}

		public Command(string name, Action defaultAction, string helpMessage)
		{
			this.name = name;
			_default = defaultAction;
			_helpMessage = helpMessage;
			Init();
		}

		protected virtual void Init()
		{
			_options = new List<Option>();
			_keywords = new List<Keyword>();
		}

		protected ICommand RegisterKeyword(string keyword, Action action)
		{
			_keywords.Add(new Keyword(keyword, action));
			return this;
		}

		protected ICommand RegisterOption(char key, Action action)
		{
			_options.Add(new Option(key, action));
			return this;
		}

		protected ICommand RegisterOption(char key, Action<string> action)
		{
			_options.Add(new Option(key, action));
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

				foreach (Option opt in _options)
				{
					foreach(char key in optKeys)
					{
						if(opt == key)
						{
							if (opt.isParamLess)
								opt.Execute();
							else
							{
								if (args.Count == 0)
									Karuta.Write("No value provided for option: " + opt.key);
								else
									opt.Execute(args[0]);
								if(args.Count != 0)
									args.RemoveAt(0);
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
