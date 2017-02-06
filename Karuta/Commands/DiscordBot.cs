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
		public AlbumEndpoint albumEndpoint;
		public string[] validExtensions = new string[] { ".gfi", ".gifv", ".png", ".jpg", ".jpeg" };
		public DiscordClient client;
		public DiscordCommandInterpreter<DiscordCommand> interpreter;

		private string _token;
		private bool _autoStart = false;
		private ImgurClient _imgurClient;
		private readonly ulong _adminUserID = 106962986572197888;
		private readonly string _appName = "Discord Bot";

		public DiscordBot() : base("discord", "a discord bot")
		{
			//Thread.CurrentThread.Name = "DiscordBot";
			_default = Init;
			_token = Karuta.REGISTY.GetString("discordToken");
			bool? auto = Karuta.REGISTY.GetBool("discordAutostart");
			_autoStart = (auto == null) ? false : (auto == true) ? true : false;
			RegisterKeyword("stop", Stop, "stops the bot");
			RegisterKeyword("autostart", () =>
			{
				_autoStart = !_autoStart;
				Karuta.REGISTY.SetValue("discordAutostart", _autoStart);
				Karuta.Write($"Autostart {((_autoStart) ? "enabled" : "disabled")}");
			}, "enable/disable autostart");

			RegisterOption('t', s =>
			{
				_token = s;
				Karuta.REGISTY.SetValue("discordToken", _token);
				Karuta.Write("Token Saved");
			}, "set the token");

			interpreter = new DiscordCommandInterpreter<DiscordCommand>()
			{
				adminUserID = _adminUserID,
				rateLimit = 5
			};

			RegisterSystemCommands();

			LoadData();

			interpreter.Init();

			init = () =>
			{
				if (_autoStart)
					_default();
			};
		}

		void RegisterSystemCommands()
		{
			interpreter.RegisterCommand(new AddImageCommand(this));
			interpreter.RegisterCommand(new DiscordHelpCommand(this));
			interpreter.RegisterCommand(new DiscordSaveCommand(this));
			interpreter.RegisterCommand(new DiscordPurgeCommand(this));
			interpreter.RegisterCommand(new RemoveImageCommand(this));
			interpreter.RegisterCommand(new SetDescriptionCommand(this));
			interpreter.RegisterCommand(new DiscordEventCommand(this));
		}

		public void ImgurSetup()
		{
			string imgID = Karuta.REGISTY.GetString("imgur_id");
			string imgSec = Karuta.REGISTY.GetString("imgur_secret");
			if (string.IsNullOrWhiteSpace(imgID) || string.IsNullOrWhiteSpace(imgSec))
			{
				Karuta.Write("Please enter Imgur API information:");
				imgID = Karuta.GetInput("Imgur API ID");
				imgSec = Karuta.GetInput("Imgur API Sec");
			}
			Karuta.LOGGER.Log("Connecting to imgur...", _appName);
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

			Karuta.REGISTY.SetValue("imgur_id", imgID);
			Karuta.REGISTY.SetValue("imgur_secret", imgSec);
		}

		public void SaveData()
		{
			string output = "";
			foreach(DiscordCommand C in interpreter.GetCommands())
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
			//Karuta.REGISTY.SetValue("discordImageCommands", (from DiscordCommand c in interpreter.GetCommands() where c.GetType() == typeof(DiscordImageCommand) select c));
		}

		void LoadData()
		{
			//TODO: Remove migration
			string oldData = Karuta.REGISTY.GetString("discordImageCommands");
			if (string.IsNullOrWhiteSpace(oldData))
				return;
			string[] cmds = oldData.Split('`');
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
				interpreter.RegisterCommand((hMessage != null) ? new DiscordImageCommand(cmdName, hMessage) { images = images} : new DiscordImageCommand(cmdName) { images = images});
			}
			/*SaveData();
			interpreter.Clear();
			RegisterSystemCommands();
			IEnumerable<DiscordCommand> data = Karuta.REGISTY.GetValue<IEnumerable<DiscordCommand>>("discordImageCommands");
			if (data == null)
				return;
			foreach(DiscordCommand c in data)
			{
				interpreter.RegisterCommand(c);
			}*/
			

		}

		async void Init()
		{
			Karuta.LOGGER.Log("Starting Discord Bot..", _appName);
			try
			{
				ImgurSetup();
			}
			catch (Exception e)
			{
				Karuta.LOGGER.LogError($"Unable to initiate Imgur Connection: {e.Message}", _appName);
			}
			client = new DiscordClient();
			if(string.IsNullOrWhiteSpace(_token))
			{
				_token = Karuta.GetInput("Enter discord token");
				Karuta.REGISTY.SetValue("discordToken", _token);
			}

			client.MessageReceived += MessageRecieved;
			client.UserJoined += UserJoined;
			try
			{
				await client.Connect(_token, TokenType.Bot);
				client.SetGame("World Domination");
				//SendToAllConsoles("Bot Online");
			}catch(Exception e)
			{
				Karuta.LOGGER.LogWarning($"Unable to initiate connection to discord: {e.Message}", _appName);
				Karuta.LOGGER.LogError(e.StackTrace, _appName, true);
				Karuta.Write("Unable to connect to discord...");
			}
		}

		private async void SendToAllGeneral(string message)
		{
			foreach(Server s in client.Servers)
			{
				await s.DefaultChannel.SendMessage(message);
			}
		}

		private async void SendToAllConsoles(string message)
		{
			foreach (Server s in client.Servers)
			{
				foreach(Channel c in s.FindChannels("console", ChannelType.Text, false))
				{
					await c.SendMessage(message);
				}
			}
		}

		private void UserJoined(object sender, UserEventArgs e)
		{
			e.Server.DefaultChannel.SendMessage($"Welcome {e.User.Name}");
		}

		public override void Stop()
		{
			/*if(client != null)
			{
				foreach(Server s in client.Servers)
				{
					Karuta.Write(s.Name);
					foreach(Channel c in s.FindChannels("console", null, true))
					{
						await c.SendMessage("Bot shutting down...");
					}
				}
			}
			Thread.Sleep(2 * 1000);*/
			Karuta.Write("Shutting down...");
			Karuta.LOGGER.Log("Shutting down bot...", _appName);
			client?.Disconnect();
			client = null;
			SaveData();
		}

		public void InvokeCommand(string command, Channel channel)
		{
			if (command[0] == '!')
				command = command.Remove(0, 1);
			interpreter.ExecuteCommands(new string[] { command }, channel, 0);

			
		}

		void MessageRecieved(object sender, MessageEventArgs e)
		{
			interpreter.Interpret(e.Message);
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
