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

		public DiscordCommand(string name, Action action, string helpMessage) : base(name, helpMessage)
		{

		}

		public DiscordCommand(string name, string helpMessage) : base(name, helpMessage)
		{

		}

		public DiscordCommand Parse(List<string> args, Channel channel)
		{
			_channel = channel;
			Parse(args);
			return this;
		}
	}

	class SetDescriptionCommand : DiscordCommand
	{
		private string _help, _name;
		private DiscordBot bot;

		public SetDescriptionCommand(DiscordBot bot) : base("set-info", "set the description of an image command")
		{
			_default = SetInfo;
			this.bot = bot;
			RegisterOption('n', n => { _name = n; }, "specify the image name");
			RegisterOption('i', i => { _help = i; }, "specify a description of the image");
		}

		async void SetInfo()
		{
			if (!string.IsNullOrWhiteSpace(_help) && !string.IsNullOrWhiteSpace(_name))
			{
				if (bot.commands.ContainsKey(_name))
				{
					if (bot.commands[_name].GetType() == typeof(DiscordImageCommand))
					{
						((DiscordImageCommand)bot.commands[_name]).SetHelpMessage(_help);
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
		private DiscordBot bot;

		public AddImageCommand(DiscordBot bot) : base("add-image", "add an image new image command or extend an existing one")
		{
			_default = Add;
			this.bot = bot;
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
				if (bot.validExtensions.Contains(Path.GetExtension(url.AbsolutePath)))
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
				imageLinks = await bot.ResolveImgurUrl(url);
			}
			//Add Command
			if (bot.commands.ContainsKey(_name))
			{
				if (bot.commands[_name].GetType() == typeof(DiscordImageCommand))
				{
					DiscordImageCommand cmd = (DiscordImageCommand)bot.commands[_name];
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
				bot.RegisterCommand(img);
				img.init?.Invoke();
				(from h in bot.commands.Values where h.GetType() == typeof(DiscordHelpCommand) select h as DiscordHelpCommand).First()?.init();
				await _channel.SendMessage($"{imageLinks.Count} Image{((imageLinks.Count > 1) ? "s" : "")} Command Added");
				Karuta.Write(img.ToString());
			}
			//foreach (string c in bot.commands.Keys)
			//	_channel.SendMessage(c);
			bot.SaveData();
			Karuta.InvokeCommand("save", new List<string>());
			_url = _name = null;
		}
	}

	class RemoveImageCommand : DiscordCommand
	{

		private string _name;
		private string _url;
		private DiscordBot bot;
		private bool _removeEntirely = false;

		public RemoveImageCommand(DiscordBot bot) : base("remove-image", "remove an image")
		{
			_default = Remove;
			this.bot = bot;
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
					if (bot.validExtensions.Contains(Path.GetExtension(url.AbsolutePath)))
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
				imageLinks = await bot.ResolveImgurUrl(url);
			}
			if (bot.commands.ContainsKey(_name))
			{
				int removed = 0, skipped = 0;
				if (_removeEntirely)
					bot.commands.Remove(_name);
				else
				{
					if (bot.commands[_name].GetType() != typeof(DiscordImageCommand))
						await _channel.SendMessage("This is not an image command");
					DiscordImageCommand cmd = ((DiscordImageCommand)bot.commands[_name]);
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
						bot.commands.Remove(_name);
				}
				await _channel.SendMessage($"{removed} Image{((removed > 1) ? "s" : "")} removed");
				if (skipped != 0)
					await _channel.SendMessage($"{skipped} Image{((skipped > 1) ? "s" : "")} were not found, and skipped");
			}
			else
				await _channel.SendMessage("that image command does not exsist");
			(from h in bot.commands.Values where h.GetType() == typeof(DiscordHelpCommand) select h as DiscordHelpCommand).First()?.init();
			bot.SaveData();
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
		DiscordBot bot;
		bool search = false;
		public DiscordHelpCommand(DiscordBot bot) : base("help", "shows this list")
		{
			this.bot = bot;
			_default = ShowHelp;

			RegisterOption('s', async s =>
			{
				search = true;
				//Karuta.Write("Searching...");
				await _channel.SendMessage("Searching...");
				string output = "";
				foreach (DiscordCommand dc in from c in bot.commands.Values where c.name.Contains(s) || c.helpMessage.Contains(s) select c)
				{
					output += $"!{dc.name} {dc.helpMessage}\n";
				}

				output = output.Replace(s, $"**{s}**");
				output = (string.IsNullOrWhiteSpace(output) ? "No commands found" : $"Search results: \n{output}");

				await _channel.SendMessage(output);
			}, "list all commands matching the search parameters");

			init = () =>
			{
				ClearKeywords();
				foreach(DiscordCommand c in from d in bot.commands.Values where d.GetType() != typeof(DiscordImageCommand) select d)
				{
					RegisterKeyword(c.name, async () =>
					{
						//Karuta.Write(c.name);
						await _channel.SendMessage($"The \"{c.name}\" command:");
						await _channel.SendMessage($"  {c.helpMessage}");
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

			imgCmd.AddRange(from c in bot.commands.Values where c.GetType() == typeof(DiscordImageCommand) select c as DiscordImageCommand);
			cmd.AddRange(from c in bot.commands.Values where c.GetType() != typeof(DiscordImageCommand) select c);

			int i = 0;
			List<string> output = new List<string>();
			output.Add($"----- - Page {(i + 1)}------\n");
			output[i] += ("The list of commands are:\n");
			output[i] += "------Bot Commands------\n";
			if (cmd.Count == 0)
				output[i] += ("There are no bot commands \n");
			else
			{
				foreach (DiscordCommand c in cmd)
					output[i] += ($"!{c.name} {c.helpMessage}\n");
				if (output[i].Length >= 1500)
				{
					i++;
					output.Add($"------Page {(i + 1)} ------\n");
				}
			}
			output[i] += "------Image Commands------\n";
			if (imgCmd.Count == 0)
				output[i] += ("There are no image commands\n");
			else
			{
				foreach (DiscordImageCommand c in imgCmd)
				{
					output[i] += ($"!{c.name} {c.helpMessage} [{c.images.Count}]\n");
					if (output[i].Length >= 1500)
					{
						i++;
						output.Add($"------Page {(i + 1)} ------\n");
					}
				}
			}
			//Karuta.Write(output[i].Length + "| " + output);
			foreach (string o in output)
				_channel.SendMessage(o);
		}
	}

	class DiscordSaveCommand : DiscordCommand
	{
		DiscordBot bot;
		public DiscordSaveCommand(DiscordBot bot) : base("force-save", "force a save")
		{
			this.bot = bot;
			_default = Save;
		}

		void Save()
		{
			Karuta.InvokeCommand("save", new List<string>());
			Karuta.Write("Data is saved:");
			string[] data = bot.SaveData().Split('`');
			foreach (string s in data)
				Karuta.Write(s);
		}
	}

	class DiscordPurgeCommand : DiscordCommand
	{
		DiscordBot bot;
		public DiscordPurgeCommand(DiscordBot bot) : base("purge", "purges all data")
		{
			this.bot = bot;
			_default = Purge;
		}

		void Purge()
		{
			Karuta.registry.SetValue("discordImageCommands", "");
			foreach (DiscordImageCommand c in from c in bot.commands.Values where c.GetType() == typeof(DiscordImageCommand) select c as DiscordImageCommand)
				bot.commands.Remove(c.name);
			Karuta.InvokeCommand("save", new List<string>());
			_channel.SendMessage("All image command data has been purged");
		}
	}
}
