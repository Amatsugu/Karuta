using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Discord;
using Imgur.API.Authentication.Impl;
using Imgur.API.Endpoints.Impl;
using System.IO;

namespace com.LuminousVector.Karuta.Commands.DiscordBot
{
	class DiscordBot : Command
	{
		public Dictionary<string, DiscordCommand> commands = new Dictionary<string, DiscordCommand>();
		public AlbumEndpoint albumEndpoint;
		public ImageEndpoint imgEndpoint;
		public string[] validExtensions = new string[] { ".gfi", ".gifv", ".png", ".jpg", ".jpeg" };


		private DiscordClient _client;
		private string _token;
		private Thread _thread;
		private bool _autoStart = false;
		private ImgurClient _imgurClient;

		public DiscordBot() : base("discord", "a discord bot")
		{
			//Thread.CurrentThread.Name = "DiscordBot";
			_default = Init;
			_token = Karuta.registry.GetString("discordToken");
			bool? auto = Karuta.registry.GetBool("discordAutostart");
			_autoStart = (auto == null) ? false : (auto == true) ? true : false;
			RegisterKeyword("stop", Stop, "stops the bot");
			RegisterKeyword("autostart", () =>
			{
				_autoStart = !_autoStart;
				Karuta.registry.SetValue("discordAutostart", _autoStart);
				Karuta.Write("Autostart " + ((_autoStart) ? "enabled" : "disabled"));
			}, "enable/disable autostart");

			RegisterOption('t', s =>
			{
				_token = s;
				Karuta.registry.SetValue("discordToken", _token);
				Karuta.Write("Token Saved");
			}, "set the token");

			try
			{
				ImgurSetup();
			}catch(Exception e)
			{
				Karuta.Write("Unable to initiate Imgur Connection: " + e.Message);
			}
			RegisterCommand(new AddImageCommand(this));
			RegisterCommand(new DiscordHelpCommand(this));
			RegisterCommand(new DiscordSaveCommand(this));
			RegisterCommand(new DiscordPurgeCommand(this));
			RegisterCommand(new RemoveImageCommand(this));
			
			LoadData();

			init = () =>
			{
				if (_autoStart)
					_default();
			};
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
			_thread = Karuta.CreateThread("DiscordBot", async () =>
			{
				_client = new DiscordClient();
				if(_token == "")
				{
					_token = Karuta.GetInput("Enter discord token");
					Karuta.registry.SetValue("discordToken", _token);
				}

				_client.MessageReceived += MessageRecieved;
				try
				{
					await _client.Connect(_token, TokenType.Bot);
				}catch(Exception e)
				{
					Karuta.Write("Unable to initiate connection to discord: " + e.Message);
					Karuta.CloseThread(_thread);
				}

			});
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
				string message = e.Message.Text.ToLower().Split(' ')[0].Remove(0, 1);
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
					}
					catch (Exception ex)
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

		public async Task<List<string>> ResolveImgurUrl(Uri url)
		{
			List<string> imageLinks = new List<string>();
			if (url.AbsolutePath.Contains("/a/") || url.AbsolutePath.Contains("/gallery/"))
			{
				//Album
				string id = url.AbsolutePath.Split('/')[2];
				Karuta.Write("album: " + id);
				if (id.Length < 3)
				{
					throw new Exception("Only Imgur URLs are supported");
				}
				var album = await albumEndpoint.GetAlbumImagesAsync(id);
				foreach (var image in album)
					imageLinks.Add(image.Link);

			}
			else if (Path.GetFileNameWithoutExtension(url.AbsolutePath) != "new")
			{
				string id = Path.GetFileNameWithoutExtension(url.AbsolutePath);
				Karuta.Write("item: " + id);
				//Indirect Imgur DL
				var image = await imgEndpoint.GetImageAsync(id);
				imageLinks.Add(image.Link);
			}

			return imageLinks;
		}
	}
}
