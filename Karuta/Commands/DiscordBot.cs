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

namespace LuminousVector.Karuta.Commands.DiscordBot
{
	[KarutaCommand(Name = "discord")]
	class DiscordBot : Command
	{
		public Dictionary<string, DiscordCommand> commands = new Dictionary<string, DiscordCommand>();
		public AlbumEndpoint albumEndpoint;
		public string[] validExtensions = new string[] { ".gfi", ".gifv", ".png", ".jpg", ".jpeg" };


		private DiscordClient _client;
		private string _token;
		private Thread _thread;
		private bool _autoStart = false;
		private ImgurClient _imgurClient;
		private readonly ulong adminUserID = 106962986572197888;
		private readonly string appName = "Discord Bot";

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
				Karuta.Write($"Autostart {((_autoStart) ? "enabled" : "disabled")}");
			}, "enable/disable autostart");

			RegisterOption('t', s =>
			{
				_token = s;
				Karuta.registry.SetValue("discordToken", _token);
				Karuta.Write("Token Saved");
			}, "set the token");

			RegisterCommand(new AddImageCommand(this));
			RegisterCommand(new DiscordHelpCommand(this));
			RegisterCommand(new DiscordSaveCommand(this));
			RegisterCommand(new DiscordPurgeCommand(this));
			RegisterCommand(new RemoveImageCommand(this));
			RegisterCommand(new SetDescriptionCommand(this));
			
			LoadData();

			foreach (DiscordCommand c in commands.Values)
			{
				c.init?.Invoke();
			}

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
			if (string.IsNullOrWhiteSpace(imgID) || string.IsNullOrWhiteSpace(imgSec))
			{
				Karuta.Write("Please enter Imgur API information:");
				imgID = Karuta.GetInput("Imgur API ID");
				imgSec = Karuta.GetInput("Imgur API Sec");
			}
			Karuta.logger.Log("Connecting to imgur...", appName);
			try
			{
				_imgurClient = new ImgurClient(imgID, imgSec);
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
			if (string.IsNullOrWhiteSpace(data))
				return;
			string[] cmds = data.Split('`');
			if (cmds.Length == 0)
				return;
			foreach(string cmd in cmds)
			{
				List<string> images = new List<string>();
				string[] helpSplit = cmd.Split('}');
				string hMessage = null;
				if (helpSplit.Length > 1)
					hMessage = helpSplit[1];
				images.AddRange(helpSplit[0].Split('{'));
				if (images.Count <= 1)
					continue;
				string cmdName = images[0];
				images.RemoveAt(0);
				RegisterCommand((hMessage != null) ? new DiscordImageCommand(cmdName, hMessage) { images = images} : new DiscordImageCommand(cmdName) { images = images});
			}

		}

		public void RegisterCommand(DiscordCommand cmd)
		{
			if (commands.ContainsKey(cmd.name))
				throw new Exception($"Command {cmd.name} already exsists!");
			commands.Add(cmd.name, cmd);
		}

		void Init()
		{
			Karuta.logger.Log("Starting Discord Bot..", appName);
			_thread = Karuta.CreateThread("DiscordBot", async () =>
			{
				try
				{
					ImgurSetup();
				}
				catch (Exception e)
				{
					Karuta.logger.LogError($"Unable to initiate Imgur Connection: {e.Message}", appName);
				}
				_client = new DiscordClient();
				if(string.IsNullOrWhiteSpace(_token))
				{
					_token = Karuta.GetInput("Enter discord token");
					Karuta.registry.SetValue("discordToken", _token);
				}

				_client.MessageReceived += MessageRecieved;
				_client.UserJoined += UserJoined;
				try
				{
					await _client.Connect(_token, TokenType.Bot);
				}catch(Exception e)
				{
					Karuta.logger.LogWarning($"Unable to initiate connection to discord: {e.Message}", appName);
					Karuta.logger.LogError(e.StackTrace, appName);
					Karuta.Write("Unable to connect to discord...");
					Karuta.CloseThread(_thread);
				}
			});
		}

		private void UserJoined(object sender, UserEventArgs e)
		{
			e.Server.DefaultChannel.SendMessage($"Welcome {e.User.Name}");
			//throw new NotImplementedException();
		}

		public override async void Stop()
		{
			if(_client != null)
			{
				foreach(Server s in _client.Servers)
				{
					Karuta.Write(s.Name);
					foreach(Channel c in s.FindChannels("console", null, true))
					{
						await c.SendMessage("Bot shutting down...");
					}
				}
			}
			Thread.Sleep(2 * 1000);
			Karuta.Write("Shutting down...");
			Karuta.logger.Log("Shutting down bot...", appName);
			_client?.Disconnect();
			_client = null;
			_thread = null;
			SaveData();
		}

		void MessageRecieved(object sender, MessageEventArgs e)
		{
			if (!e.Message.IsAuthor && e.Message?.Text?.Length > 0 && e.Message?.Text?[0] == '!')
			{
				string message = e.Message.Text.Remove(0, 1);
				string[] cmds = message.Split('&');
				foreach (string command in cmds)
				{
					string cName = (from a in command.ToLower().Split(' ') where !string.IsNullOrWhiteSpace(a) select a).First();

					Karuta.logger.Log($"Command recieved: \"{cName}\" from \"{e.Message.User.Name}\" in channel \"{e.Channel.Name}\" on server \"{e.Channel.Server.Name}\"", appName);
					if (commands.ContainsKey(cName))
					{
						try
						{
							DiscordCommand cmd = commands[cName];
							if (cmd.GetType() != typeof(DiscordImageCommand))
							{
								if ((e.Channel.Name == "console" || cmd.GetType() == typeof(DiscordHelpCommand)))
								{
									if (cmd.GetType() == typeof(DiscordPurgeCommand))
									{
										if (e.Message.User.Id == adminUserID && e.Channel.Name == "console")
											cmd.Parse(new List<string>(), e.Channel);
										else
										{
											e.Channel.SendMessage("You are not authorized to use this command");
											Karuta.logger.LogWarning($"Underprivilaged user \"{e.User.Name}\" attempted to use command \"{cName}\"", appName);
										}
									}
									else
									{
										List<string> args = new List<string>();
										args.AddRange(from arg in command.Split(' ') where !string.IsNullOrWhiteSpace(arg) select arg);
										args.RemoveAt(0);
										cmd.Parse(args, e.Channel);
									}
								}
								else
								{
									e.Channel.SendMessage("this command can only be used in the console");
								}
							}
							else
								((DiscordImageCommand)cmd).SendImage(e.Channel);
						}
						catch (Exception ex)
						{
							e.Channel.SendMessage($"An error occured while executing the command: {ex.Message}");
							Karuta.logger.LogError($"An error occured while executing the command: {ex.Message}", appName);
							Karuta.logger.LogError(ex.StackTrace, appName);
						}
					}
					else
						e.Channel.SendMessage("No such command");
				}
			}
		}

		public async Task<List<string>> ResolveImgurUrl(Uri url)
		{
			List<string> imageLinks = new List<string>();
			if (url.AbsolutePath.Contains("/a/") || url.AbsolutePath.Contains("/gallery/"))
			{
				//Album
				string id = url.AbsolutePath.Split('/')[2];
				//Karuta.Write($"album: {id}");
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
				//Karuta.Write($"item: {id}");
				//Indirect Imgur DL
				//var image = await imgEndpoint.GetImageAsync(id);
				imageLinks.Add($"http://i.imgur.com/{id}.png");
			}

			return imageLinks;
		}
	}
}
