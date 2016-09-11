using System;
using System.Collections.Generic;
using System.Linq;
using Discord;
using System.IO;

namespace com.LuminousVector.Karuta.Commands.DiscordBot
{
	class DiscordCommand : Command
	{
		protected Channel _channel;

		public DiscordCommand(string name, Action action, string helpMessage) : base(name, action, helpMessage)
		{

		}

		public DiscordCommand(string name, string helpMessage) : base(name, helpMessage)
		{

		}

		public DiscordCommand Pharse(List<string> args, Channel channel)
		{
			_channel = channel;
			Pharse(args);
			return this;
		}
	}

	class AddImageCommand : DiscordCommand
	{

		private string _name;
		private string _url;
		private string _help;
		private DiscordBot bot;

		public AddImageCommand(DiscordBot bot) : base("add-image", "add an image new image command or extend an existing one")
		{
			_default = Add;
			this.bot = bot;
			RegisterOption('n', s => { _name = s; }, "specify the image name");
			RegisterOption('u', s => { _url = s; }, "specify the url of the image");
			RegisterOption('h', s => { _help = s; }, "specify a description of the image");
		}

		async void Add()
		{
			//Validate
			if (_name == null || _url == null || _url == "" || _name == "")
				await _channel.SendMessage("An image name and url must be specified");
			if (_url.Contains('`') || _url.Contains('{'))
				await _channel.SendMessage("Invalid URL");
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
						await _channel.SendMessage(dupeCount + " Image" + ((dupeCount > 1) ? "s" : "") + " already existed and were not added");
					await _channel.SendMessage(addCount + " Image" + ((addCount > 1) ? "s" : "") + " Added");
					//await _channel.SendMessage(cmd.ToString());
				}
				else
				{
					await _channel.SendMessage("This name cannot be used");
				}
			}
			else
			{
				int count = imageLinks.Count;
				//Karuta.Write(count);
				DiscordImageCommand img = new DiscordImageCommand(_name).AddImage(imageLinks[0]);
				imageLinks.RemoveAt(0);
				foreach (string link in imageLinks)
					img.AddImage(link);
				bot.RegisterCommand(img);
				await _channel.SendMessage(count + " Image" + ((count > 1) ? "s" : "") + " Command Added");
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
				await _channel.SendMessage(removed + " Image" + ((removed > 1) ? "s" : "") + " removed");
				if (skipped != 0)
					await _channel.SendMessage(skipped + "Image" + ((skipped > 1) ? "s" : "") + " were not found, and skipped");
			}
			else
				await _channel.SendMessage("that image command does not exsist");
			bot.SaveData();
			Karuta.InvokeCommand("save", new List<string>());
			_removeEntirely = false;
			_url = _name = null;
		}
	}

	class DiscordImageCommand : DiscordCommand
	{
		public List<string> images = new List<string>();

		public DiscordImageCommand(string name) : base(name, "shows image(s) of " + name)
		{

		}

		public DiscordImageCommand(string name, List<string> images) : base(name, "shows image(s) of " + name)
		{
			if (images.Count == 0)
				return;
			this.images = images;
		}

		public async void SendImage(Channel channel)
		{
			if (images.Count == 0)
				return;
			await channel.SendMessage(Utils.Random(images));
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
		public DiscordHelpCommand(DiscordBot bot) : base("help", "shows this list")
		{
			this.bot = bot;
			_default = ShowHelp;
		}

		void ShowHelp()
		{
			List<DiscordImageCommand> imgCmd = new List<DiscordImageCommand>();
			List<DiscordCommand> cmd = new List<DiscordCommand>();
			foreach (DiscordCommand c in bot.commands.Values)
				if (c.GetType() == typeof(DiscordImageCommand))
					imgCmd.Add((DiscordImageCommand)c);
				else
					cmd.Add(c);
			int i = 0;
			List<string> output = new List<string>();
			output.Add("----- - Page " + (i + 1) + "------\n");
			output[i] += ("The list of commands are:\n");
			output[i] += "------Bot Commands------\n";
			if (cmd.Count == 0)
				output[i] += ("There are no bot commands \n");
			else
			{
				foreach (DiscordCommand c in cmd)
					output[i] += ("!" + c.name + "\t" + c.helpMessage + "\n");
				if (output[i].Length >= 1500)
				{
					i++;
					output.Add("------Page " + (i + 1) + "------\n");
				}
			}
			output[i] += "------Image Commands------\n";
			if (imgCmd.Count == 0)
				output[i] += ("There are no image commands\n");
			else
			{
				foreach (DiscordImageCommand c in imgCmd)
				{
					output[i] += ("!" + c.name + "\t" + c.helpMessage + " [" + c.images.Count + "]\n");
					if (output[i].Length >= 1500)
					{
						i++;
						output.Add("------Page " + (i + 1) + "------\n");
					}
				}
			}
			Karuta.Write(output[i].Length + "| " + output);
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
			Karuta.InvokeCommand("save", new List<string>());
			List<DiscordImageCommand> removeList = new List<DiscordImageCommand>();
			foreach (DiscordCommand c in bot.commands.Values)
				if (c.GetType() == typeof(DiscordImageCommand))
					removeList.Add((DiscordImageCommand)c);
			foreach (DiscordImageCommand c in removeList)
				bot.commands.Remove(c.name);
			_channel.SendMessage("All image command data has been purged");
		}
	}
}
