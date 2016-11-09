using System;
using System.Collections.Generic;
using System.Linq;
using Discord;
using System.IO;

namespace LuminousVector.Karuta.Commands.DiscordBot
{
	class DiscordCommand : Command
	{
		protected Channel _channel;
		protected DiscordBot _bot;

		public DiscordCommand(string name, Action action, string helpMessage) : base(name, helpMessage)
		{

		}

		public DiscordCommand(string name, string helpMessage) : base(name, helpMessage)
		{

		}

		public virtual DiscordCommand Parse(List<string> args, Channel channel)
		{
			_channel = channel;
			Parse(args);
			return this;
		}
	}

	class SetDescriptionCommand : DiscordCommand
	{
		private string _help, _name;

		public SetDescriptionCommand(DiscordBot bot) : base("set-info", "set the description of an image command")
		{
			_default = SetInfo;
			_bot = bot;
			RegisterOption('n', n => { _name = n; }, "specify the image name");
			RegisterOption('i', i => { _help = i; }, "specify a description of the image");
		}

		async void SetInfo()
		{
			if (!string.IsNullOrWhiteSpace(_help) && !string.IsNullOrWhiteSpace(_name))
			{
				if (_bot.interpreter.commands.ContainsKey(_name))
				{
					if (_bot.interpreter.commands[_name].GetType() == typeof(DiscordImageCommand))
					{
						((DiscordImageCommand)_bot.interpreter.commands[_name]).SetHelpMessage(_help);
						await _channel.SendMessage("Description set");
					}
					else
						await _channel.SendMessage("This is not an image command");

				}
			}
			else
				await _channel.SendMessage($"A name and description must be provided, use !help {name} for more info");

		}
	}

	class AddImageCommand : DiscordCommand
	{

		private string _name, _url, _help;

		public AddImageCommand(DiscordBot bot) : base("add-image", "add an image new image command or extend an existing one")
		{
			_default = Add;
			_bot = bot;
			RegisterOption('n', s => { _name = s; }, "specify the image name");
			RegisterOption('u', s => { _url = s; }, "specify the url of the image");
			RegisterOption('i', s => { _help = s; }, "specify a description of the image");
		}

		async void Add()
		{
			//Validate
			if (string.IsNullOrWhiteSpace(_name) && string.IsNullOrWhiteSpace(_url))
			{
				await _channel.SendMessage($"An image name and url must be specified, use !help {name} for more info");
				return;
			}

			if ((_url.Contains('`') || _url.Contains('{')))
			{
				await _channel.SendMessage("Invalid URL");
				return;
			}

			List<string> imageLinks = new List<string>();
			Uri url = new Uri(_url);

			if (url.Host == "i.imgur.com")
			{
				if (_bot.validExtensions.Contains(Path.GetExtension(url.AbsolutePath)))
					imageLinks.Add(_url);
			}
			else if (url.Host != "imgur.com")
			{
				await _channel.SendMessage("Only imgur images are allowed");
				return;
			}

			//Resolve Link
			if (imageLinks.Count == 0)
			{
				imageLinks = await _bot.ResolveImgurUrl(url);
			}
			//Add Command
			if (_bot.interpreter.commands.ContainsKey(_name))
			{
				if (_bot.interpreter.commands[_name].GetType() == typeof(DiscordImageCommand))
				{
					DiscordImageCommand cmd = (DiscordImageCommand)_bot.interpreter.commands[_name];
					int dupeCount = 0;
					int addCount = 0;
					if (!string.IsNullOrWhiteSpace(_help))
						cmd.SetHelpMessage(_help);
					foreach (string link in imageLinks)
					{
						if (cmd.images.Contains(link))
						{
							dupeCount++;
							continue;
						}
						cmd.AddImage(link);
						addCount++;
					}
					if (dupeCount > 0)
						await _channel.SendMessage($"{dupeCount} Image{((dupeCount > 1) ? "s" : "")} already existed and were not added");
					await _channel.SendMessage($"{addCount} Image{((addCount > 1) ? "s" : "")} Added");
					//await _channel.SendMessage(cmd.ToString());
				}
				else
				{
					await _channel.SendMessage("This name cannot be used");
				}
			}
			else
			{
				//Karuta.Write(count);
				DiscordImageCommand img = (_help != default(string)) ? new DiscordImageCommand(_name, _help) { images = imageLinks } :new DiscordImageCommand(_name) { images = imageLinks};
				_bot.interpreter.RegisterCommand(img);
				img.init?.Invoke();
				(from h in _bot.interpreter.commands.Values where h.GetType() == typeof(DiscordHelpCommand) select h as DiscordHelpCommand).First()?.init();
				await _channel.SendMessage($"{imageLinks.Count} Image{((imageLinks.Count > 1) ? "s" : "")} Command Added");
				Karuta.Write(img.ToString());
			}
			//foreach (string c in bot.commands.Keys)
			//	_channel.SendMessage(c);
			_bot.SaveData();
			Karuta.InvokeCommand("save", new List<string>());
			_url = _name = null;
		}
	}

	class RemoveImageCommand : DiscordCommand
	{

		private string _name;
		private string _url;
		private bool _removeEntirely = false;

		public RemoveImageCommand(DiscordBot bot) : base("remove-image", "remove an image")
		{
			_default = Remove;
			_bot = bot;
			RegisterOption('n', s => { _name = s; }, "specify the image name");
			RegisterOption('u', s => { _url = s; }, "specify the url of the image");
			RegisterOption('f', s => { _removeEntirely = true; }, "removes the entire command");

		}

		async void Remove()
		{
			//Validate
			List<string> imageLinks = new List<string>();
			Uri url = new Uri(_url);
			if (!_removeEntirely)
			{
				if (_name == null || _url == null || _url == "" || _name == "")
				{
					await _channel.SendMessage("An image name and url must be specified");
					return;
				}
				if (_url.Contains('`') || _url.Contains('{'))
				{
					await _channel.SendMessage("Invalid URL");
					return;
				}

				if (url.Host == "i.imgur.com")
				{
					if (_bot.validExtensions.Contains(Path.GetExtension(url.AbsolutePath)))
						imageLinks.Add(_url);
				}
				else if (url.Host != "imgur.com")
				{
					await _channel.SendMessage("Only imgur images are allowed");
					return;
				}
			}
			else if (_name == null || _name == "")
			{
				await _channel.SendMessage("An image name must be specified");
				return;
			}
			//Resolve Link
			if (imageLinks.Count == 0 && !_removeEntirely)
			{
				imageLinks = await _bot.ResolveImgurUrl(url);
			}
			if (_bot.interpreter.commands.ContainsKey(_name))
			{
				int removed = 0, skipped = 0;
				if (_removeEntirely)
					_bot.interpreter.commands.Remove(_name);
				else
				{
					if (_bot.interpreter.commands[_name].GetType() != typeof(DiscordImageCommand))
						await _channel.SendMessage("This is not an image command");
					DiscordImageCommand cmd = ((DiscordImageCommand)_bot.interpreter.commands[_name]);
					foreach (string i in cmd.images)
						Karuta.Write(i);
					foreach (string u in imageLinks)
					{
						if (cmd.RemoveImage(u))
							removed++;
						else
							skipped++;
					}
					foreach (string i in cmd.images)
						Karuta.Write(i);
					if (cmd.images.Count == 0)
						_bot.interpreter.commands.Remove(_name);
				}
				await _channel.SendMessage($"{removed} Image{((removed > 1) ? "s" : "")} removed");
				if (skipped != 0)
					await _channel.SendMessage($"{skipped} Image{((skipped > 1) ? "s" : "")} were not found, and skipped");
			}
			else
				await _channel.SendMessage("that image command does not exsist");
			(from h in _bot.interpreter.commands.Values where h.GetType() == typeof(DiscordHelpCommand) select h as DiscordHelpCommand).First()?.init();
			_bot.SaveData();
			Karuta.InvokeCommand("save", new List<string>());
			_removeEntirely = false;
			_url = _name = null;
		}
	}

	class DiscordImageCommand : DiscordCommand
	{
		public List<string> images = new List<string>();
		private bool _defaultHelp = true;

		public DiscordImageCommand(string name) : base(name, $"shows image(s) of {name}")
		{

		}

		public DiscordImageCommand(string name, string helpMessage) : base(name, helpMessage)
		{
			_defaultHelp = false;
		}

		public DiscordCommand SetHelpMessage(string helpMessage)
		{
			_helpMessage = helpMessage;
			return this;
		}

		public async void SendImage(Channel channel)
		{
			if (images.Count == 0)
				return;
			await channel.SendMessage(images.GetRandom());
		}

		public DiscordImageCommand AddImage(string image)
		{
			images.Add(image);
			return this;
		}

		public DiscordImageCommand AddImage(string[] images)
		{
			this.images.AddRange(images);
			return this;
		}

		public override string ToString()
		{
			string output = name;
			foreach (string s in images)
				output += "{" + s;
			if(!_defaultHelp)
				output += "}" + helpMessage;
			return output;
		}

		internal bool RemoveImage(string image)
		{
			if (images.Contains(image))
			{
				images.Remove(image);
				return true;
			}
			else
				return false;
		}
	}

	class DiscordHelpCommand : DiscordCommand
	{
		bool search = false;
		public DiscordHelpCommand(DiscordBot bot) : base("help", "shows this list")
		{
			_bot = bot;
			_default = ShowHelp;

			RegisterOption('s', async s =>
			{
				search = true;
				//Karuta.Write("Searching...");
				await _channel.SendMessage("Searching...");
				List<string> output = new List<string>();
				output.Add("");
				int index = 0;
				string curLine = "";
				foreach (DiscordImageCommand dc in from c in bot.interpreter.commands.Values orderby c.name where c.GetType() == typeof(DiscordImageCommand) && (c.name.Contains(s) || c.helpMessage.Contains(s)) select c)
				{
					curLine = $"!{dc.name} {dc.helpMessage} [{dc.images.Count}]\n";
					if (curLine.Length + output[index].Length >= 1500)
					{
						index++;
						output.Add("");
					}
					output[index] += curLine;
				}
				for (int i = 0; i < output.Count; i++)
				{
					output[i] = output[i].Replace(s, $"**{s}**");
				}
				output[0] = ((string.IsNullOrWhiteSpace(output[0]) && output.Count == 1) ? "No commands found" : $"Search results: \n Page 1 of {output.Count}\n{output[0]}");
				for (int i = 0; i < output.Count; i++)
					await _channel.SendMessage($"{((i != 0) ? $"Page {i + 1} of {output.Count}\n" : "")} {output[i]}");
			}, "list all commands matching the search parameters");

			init = () =>
			{
				ClearKeywords();
				foreach(DiscordCommand c in from d in bot.interpreter.commands.Values orderby d.name where d.GetType() != typeof(DiscordImageCommand) select d)
				{
					RegisterKeyword(c.name, async () =>
					{
						//Karuta.Write(c.name);
						await _channel.SendMessage($"The \"{c.name}\" command:");
						string output = "";
						output += $"  {c.helpMessage} \n";
						foreach (Keyword k in c.keywords)
							output += $"   {k.keyword} {k.usage}\n";
						foreach (Option o in c.options)
							output += $"   -{o.key} {o.usage}\n";
						await _channel.SendMessage(output);
					});
				}
			};
		}

		public override ICommand Parse(List<string> args)
		{
			search = false;
			return base.Parse(args);
		}

		void ShowHelp()
		{
			if (search)
				return;
			List<DiscordImageCommand> imgCmd = new List<DiscordImageCommand>();
			List<DiscordCommand> cmd = new List<DiscordCommand>();

			imgCmd.AddRange(from c in _bot.interpreter.commands.Values orderby c.name where c.GetType() == typeof(DiscordImageCommand) select c as DiscordImageCommand);
			cmd.AddRange(from c in _bot.interpreter.commands.Values orderby c.name where c.GetType() != typeof(DiscordImageCommand) select c);

			int index = 0;
			List<string> output = new List<string>();
			output.Add("");
			output[index] += ("The list of commands are:\n");
			output[index] += "------Bot Commands------\n";
			if (cmd.Count == 0)
				output[index] += ("There are no bot commands \n");
			else
			{
				string curLine = "";
				foreach (DiscordCommand c in cmd)
				{
					curLine = ($"!{c.name} {c.helpMessage}\n");
					if(curLine.Length + output[index].Length > 1500)
					{
						index++;
						output.Add("");
					}
					output[index] += curLine;
				}
			}
			index++;
			output.Add("------Image Commands------\n");
			if (imgCmd.Count == 0)
				output[index] += ("There are no image commands\n");
			else
			{
				string curLine = "";
				foreach (DiscordImageCommand c in imgCmd)
				{
					curLine = ($"!{c.name} {c.helpMessage} [{c.images.Count}]\n");
					if (curLine.Length + output[index].Length >= 1500)
					{
						index++;
						output.Add("");
					}
					output[index] += curLine;
				}
			}
			//Karuta.Write(output[i].Length + "| " + output);
			for(int i = 0; i < output.Count; i++)
			{
				_channel.SendMessage($"{(i != 0 ? $"----- - Page {(i + 1)} of {output.Count}------\n" : "")} {output[i]}" );
			}
		}
	}

	class DiscordSaveCommand : DiscordCommand
	{
		public DiscordSaveCommand(DiscordBot bot) : base("force-save", "force a save")
		{
			_bot = bot;
			_default = Save;
		}

		async void Save()
		{
			await _channel.SendMessage("Saving Data...");
			Karuta.InvokeCommand("save", new List<string>());
			Karuta.Write("Data is saved:");
			string[] data = _bot.SaveData().Split('`');
			foreach (string s in data)
				Karuta.Write(s);
			await _channel.SendMessage("Saved!");
		}
	}

	class DiscordPurgeCommand : DiscordCommand
	{
		public DiscordPurgeCommand(DiscordBot bot) : base("purge", "purges all data")
		{
			_bot = bot;
			_default = Purge;
		}

		void Purge()
		{
			Karuta.registry.SetValue("discordImageCommands", "");
			foreach (DiscordImageCommand c in from c in _bot.interpreter.commands.Values where c.GetType() == typeof(DiscordImageCommand) select c as DiscordImageCommand)
				_bot.interpreter.commands.Remove(c.name);
			Karuta.InvokeCommand("save", new List<string>());
			_channel.SendMessage("All image command data has been purged");
		}
	}
}
