using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using Discord;
using Imgur.API.Authentication.Impl;
using Imgur.API.Endpoints.Impl;
using System.IO;

namespace com.LuminousVector.Karuta.Commands
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
		private DiscordBot bot;
		private string[] _validExtensions = new string[] { ".gfi", ".gifv", ".png", ".jpg", ".jpeg" };

		public AddImageCommand(DiscordBot bot) : base("add-image", "add an image new image command or extend an existing one")
		{
			_default = Add;
			this.bot = bot;
			RegisterOption('n', s => { _name = s; }, "specify the image name");
			RegisterOption('u', s => { _url = s; }, "specify the url of the image");
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

			if(url.Host == "i.imgur.com")
			{
				if (_validExtensions.Contains(Path.GetExtension(url.AbsolutePath)))
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
				if (url.AbsolutePath.Contains("/a/") || url.AbsolutePath.Contains("/gallery/"))
				{
					//Album
					string id = url.AbsolutePath.Split('/')[2];
					Karuta.Write("album: " + id);
					if (id.Length < 3)
					{
						await _channel.SendMessage("Invalid album URL");
						return;
					}
					try
					{
						var album = await bot.albumEndpoint.GetAlbumImagesAsync(id);
						foreach (var image in album)
						{
							imageLinks.Add(image.Link);
						}
					}
					catch (Exception e)
					{
						await _channel.SendMessage("An error occured: " + e.Message);
						Karuta.Write(e.Message);
						Karuta.Write(e.StackTrace);
					}

				}
				else if (Path.GetFileNameWithoutExtension(url.AbsolutePath) != "new")
				{
					string id = Path.GetFileNameWithoutExtension(url.AbsolutePath);
					Karuta.Write("item: " + id);
					//Indirect Imgur DL
					try
					{
						var image = await bot.imgEndpoint.GetImageAsync(id);
						imageLinks.Add(image.Link);
					}
					catch (Exception e)
					{
						await _channel.SendMessage("An error occured: " + e.Message);
						Karuta.Write(e.Message);
						Karuta.Write(e.StackTrace);
					}
				}
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
					if(dupeCount > 0)
						await _channel.SendMessage(dupeCount + " Image(s) already existed and were not added");
					await _channel.SendMessage(addCount + " Image(s) Added");
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
				await _channel.SendMessage(count + " Image(s) Command Added");
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

		void Remove()
		{
			if ((_name == null || _url == null) && !_removeEntirely)
				_channel.SendMessage("An image name and url must be specified");
			if (bot.commands.ContainsKey(_name))
			{
				if (_removeEntirely)
					bot.commands.Remove(_name);
				else
				{
					if (bot.commands[_name].GetType() != typeof(DiscordImageCommand))
						_channel.SendMessage("This is not an image command");
					DiscordImageCommand cmd = ((DiscordImageCommand)bot.commands[_name]);
					foreach (string i in cmd.images)
						Karuta.Write(i);
					cmd.RemoveImage(_url);
					foreach (string i in cmd.images)
						Karuta.Write(i);
					if (cmd.images.Count == 0)
						bot.commands.Remove(_name);
				}
				_channel.SendMessage("Image removed");
			}
			else
				_channel.SendMessage("that image command does not exsist");
			bot.SaveData();
			Karuta.InvokeCommand("save", new List<string>());
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

		internal void RemoveImage(string image)
		{
			images.Remove(image);
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
			foreach(DiscordCommand c in bot.commands.Values)
				if (c.GetType() == typeof(DiscordImageCommand))
					imgCmd.Add((DiscordImageCommand)c);
				else
					cmd.Add(c);
			int i = 0;
			List<string> output = new List<string>();
			output.Add("----- - Page " + (i+1) + "------\n");
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
					output.Add("------Page " + (i+1) + "------\n");
				}
			}
			output[i] += "------Image Commands------\n";
			if (imgCmd.Count == 0)
				output[i] += ("There are no image commands\n");
			else
			{
				foreach (DiscordImageCommand c in imgCmd)
				{
					output[i] += ("!" + c.name + "\t" + c.helpMessage + " [" + c.images.Count +"]\n");
					if (output[i].Length >= 1500)
					{
						i++;
						output.Add("------Page " + (i + 1) + "------\n");
					}
				}
			}
			Karuta.Write(output[i].Length + "| " + output);
			foreach(string o in output)
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
			foreach(string s in data)
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

	class DiscordBot : Command
	{
		public Dictionary<string, DiscordCommand> commands = new Dictionary<string, DiscordCommand>();
		public AlbumEndpoint albumEndpoint;
		public ImageEndpoint imgEndpoint;

		private DiscordClient _client;
		private string _token;
		private Thread _thread;
		private bool _autoStart = true;
		private ImgurClient _imgurClient;

		public DiscordBot() : base("discord", "a discord bot")
		{
			//Thread.CurrentThread.Name = "DiscordBot";
			_default = Init;
			_token = Karuta.registry.GetString("discordToken");

			RegisterKeyword("stop", Stop, "stops the bot");
			RegisterOption('t', s =>
			{
				_token = s;
				Karuta.registry.SetValue("discordToken", _token);
			}, "set the token");
			ImgurSetup();
			RegisterCommand(new AddImageCommand(this));
			RegisterCommand(new DiscordHelpCommand(this));
			RegisterCommand(new DiscordSaveCommand(this));
			RegisterCommand(new DiscordPurgeCommand(this));
			RegisterCommand(new RemoveImageCommand(this));
			
			LoadData();

			if (_autoStart)
				_default();
		}

		public void ImgurSetup()
		{
			string imgID = Karuta.registry.GetString("imgur_id");
			string imgSec = Karuta.registry.GetString("imgur_secret");
			if (imgID == "" || imgSec == "")
			{
				Karuta.Write("Please enter Imgur API information:");
				imgID = Karuta.GetInput("Imgur API ID");
				imgSec = Karuta.GetInput("Imgur API Sec");
			}
			Karuta.Write("Connecting to imgur...");
			try
			{
				_imgurClient = new ImgurClient(imgID, imgSec);
				imgEndpoint = new ImageEndpoint(_imgurClient);
				albumEndpoint = new AlbumEndpoint(_imgurClient);
			}
			catch (Exception e)
			{
				Karuta.Write("Failed to connect");
				Karuta.Write(e.Message);
				_imgurClient = null;
			}

			Karuta.registry.SetValue("imgur_id", imgID);
			Karuta.registry.SetValue("imgur_secret", imgSec);
		}

		public string SaveData()
		{
			string output = "";
			foreach(DiscordCommand C in commands.Values)
			{
				if (C.GetType() == typeof(DiscordImageCommand))
				{
					DiscordImageCommand cmd = (DiscordImageCommand)C;
					if (output == "")
						output += cmd.ToString();
					else
						output += "`" + cmd.ToString();

				}
			}
			Karuta.registry.SetValue("discordImageCommands", output);
			return output;
		}

		void LoadData()
		{
			string data = Karuta.registry.GetString("discordImageCommands");
			if (data == "")
				return;
			string[] cmds = data.Split('`');
			if (cmds.Length == 0)
				return;
			foreach(string cmd in cmds)
			{
				List<string> images = new List<string>();
				images.AddRange(cmd.Split('{'));
				if (images.Count <= 1)
					continue;
				string cmdName = images[0];
				images.RemoveAt(0);
				RegisterCommand(new DiscordImageCommand(cmdName, images));
			}

		}

		public void RegisterCommand(DiscordCommand cmd)
		{
			if (commands.ContainsKey(cmd.name))
				throw new Exception("Command " + cmd.name + " already exsists!");
			commands.Add(cmd.name, cmd);
		}

		void Init()
		{
			_thread = new Thread(async () =>
			{
				_client = new DiscordClient();
				if(_token == "")
				{
					_token = Karuta.GetInput("Enter discord token");
					Karuta.registry.SetValue("discordToken", _token);
				}

				_client.MessageReceived += MessageRecieved;
				await _client.Connect(_token);

			});
			_thread.Start();
		}

		public override void Stop()
		{
			_thread?.Join();
			_thread = null;
			SaveData();
		}

		void MessageRecieved(object sender, MessageEventArgs e)
		{
			if (!e.Message.IsAuthor && e.Message.Text.Length > 0 && e?.Message?.Text?[0] == '!')
			{
				string message = e.Message.Text.ToLower().Split(' ')[0].Remove(0,1);
				Karuta.Write(message);
				if (commands.ContainsKey(message))
				{
					try
					{
						DiscordCommand cmd = commands[message];
						if (cmd.GetType() == typeof(DiscordImageCommand))
							((DiscordImageCommand)cmd).SendImage(e.Channel);
						else if (cmd.GetType() == typeof(DiscordHelpCommand))
							cmd.Pharse(new List<string>(), e.Channel);
						else if (cmd.GetType() == typeof(DiscordPurgeCommand))
						{
							if (e.Message.User.Id == 106962986572197888 && e.Channel.Name == "console")
								cmd.Pharse(new List<string>(), e.Channel);
							else
								e.Channel.SendMessage("You are not authorized to use this command");
						}
						else
						{
							if (e.Channel.Name == "console")
							{
								List<string> args = new List<string>();
								args.AddRange(e.Message.Text.Split(' '));
								args.RemoveAt(0);
								cmd.Pharse(args, e.Channel);
							}
							else
							{
								e.Channel.SendMessage("this command can only be used in the console");
							}
						}
					}catch(Exception ex)
					{
						e.Channel.SendMessage("An error occured while executing the command: " + ex.Message);
						Karuta.Write("An error occured while executing the command: " + ex.Message);
						Karuta.Write(ex.StackTrace);
					}
				}
				else
					e.Channel.SendMessage("No such command");
			}
		}
	}
}
