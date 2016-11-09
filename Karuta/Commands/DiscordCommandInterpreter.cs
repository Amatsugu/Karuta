using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord;
using LuminousVector.Karuta.Commands;

namespace LuminousVector.Karuta.Commands.DiscordBot
{
	class UserCommandRateInfo
	{
		public int useCount { get; set; }
		public DateTime lastUsage { get; set; }
		public DateTime timeoutEnd { get; set; }

		public UserCommandRateInfo(int useCount, DateTime lastUsage)
		{
			this.useCount = useCount;
			this.lastUsage = lastUsage;
		}
	}

	class DiscordCommandInterpreter<T> : CommandInterpreter<T> where T : Command
	{
		public ulong adminUserID;
		public int rateLimit = 5;
		public Dictionary<ulong, UserCommandRateInfo> userCommandRate = new Dictionary<ulong, UserCommandRateInfo>();
		public float timeoutDuration = 60;
		public float minTimeRange = 10;
		public float minCommandRate = 5;

		public void Interpret(Message message)
		{
			if (!message.IsAuthor && message?.Text?.Length > 0)
			{
				string text = message.Text;
				if (message.Channel.Name != "console")
				{
					if (!text.Contains(" ") && !text.Contains("&") && _commands.ContainsKey(text.ToLower())) //One word commands
					{
						ExecuteCommands(text.Split('&'), message.Channel, message.User.Id);
						return;
					}
					else if (message?.Text?[0] == '!') //!commands
					{
						text = message.Text.Remove(0, 1);
						ExecuteCommands(text.Split('&'), message.Channel, message.User.Id);
						return;
					}
				}
				else
				{

					if (message?.Text?[0] == '!')//Console Commands
					{
						text = message.Text.Remove(0, 1);
					}
					ExecuteCommands(text.Split('&'), message.Channel, message.User.Id);
				}
			}

		}

		public bool IsImageCommand(string command)
		{
			if (!_commands.ContainsKey(command))
				return false;
			return _commands[command].GetType() == typeof(DiscordImageCommand);
		}

		public async void ExecuteCommands(string[] commands, Channel channel, ulong user)
		{
			string username = channel.GetUser(user).Name;
			if (!userCommandRate.ContainsKey(user))
				userCommandRate.Add(user, new UserCommandRateInfo(0, DateTime.Now));
			if (channel.Name != "console" && userCommandRate[user].useCount > rateLimit)
			{
				double timeoutTime = (userCommandRate[user].timeoutEnd - DateTime.Now).TotalSeconds;
				if (timeoutTime > 0)
				{
					await channel.SendMessage($"@{username}: Please wait {Utils.SmartRound(timeoutTime, 100)} seconds before using another command.");
					return;
				}
				else
				{
					userCommandRate[user].useCount = 0;
				}
			}
			foreach (string command in commands)
			{
				if((DateTime.Now - userCommandRate[user].lastUsage).TotalSeconds < minTimeRange)
				{
					userCommandRate[user].useCount++;
				}else
				{
					userCommandRate[user].useCount = 0;
				}
				userCommandRate[user].lastUsage = DateTime.Now;
				//Limit Command usage
				if (user != 0 && userCommandRate[user].useCount > rateLimit && channel.Name != "console")
				{
					userCommandRate[user].timeoutEnd = DateTime.Now.AddSeconds(timeoutDuration);
					await channel.SendMessage($"@{username}: Rate Limit reached, aborting... You have been timed out for {timeoutDuration} seconds");
					break;
				}
				string cName = (from a in command.ToLower().Split(' ') where !string.IsNullOrWhiteSpace(a) select a).First();
				Karuta.logger.Log($"Command recieved: \"{cName}\" from \"{(user == 0 ? "EventBot" : $"{ channel.GetUser(user)?.Name}")}\" in channel \"{channel.Name}\" on server \"{channel.Server.Name}\"", nameof(DiscordCommandInterpreter<T>));
				if (_commands.ContainsKey(cName))
				{
					try
					{
						DiscordCommand cmd = _commands[cName] as DiscordCommand;
						if (cmd.GetType() != typeof(DiscordImageCommand) && user != 0)
						{
							if ((channel.Name == "console" || cmd.GetType() == typeof(DiscordHelpCommand)))
							{
								if (cmd.GetType() == typeof(DiscordPurgeCommand))
								{
									if (user == adminUserID && channel.Name == "console")
										cmd.Parse(new List<string>(), channel);
									else
									{
										await channel.SendMessage("You are not authorized to use this command");
										Karuta.logger.LogWarning($"Underprivilaged user \"{channel.GetUser(user)?.Name}\" attempted to use command \"{cName}\"", nameof(DiscordCommandInterpreter<T>));
									}
								}
								else
								{
									List<string> args = new List<string>();
									args.AddRange(from arg in command.Split(' ') where !string.IsNullOrWhiteSpace(arg) select arg);
									args.RemoveAt(0);
									cmd.Parse(args, channel);
								}
							}
							else
							{
								await channel.SendMessage("this command can only be used in the console");
							}
						}
						else
							((DiscordImageCommand)cmd).SendImage(channel);
					}
					catch (Exception ex)
					{
						await channel.SendMessage($"An error occured while executing the command: {ex.Message}");
						Karuta.logger.LogError($"An error occured while executing the command: {ex.Message}", nameof(DiscordCommandInterpreter<T>));
						Karuta.logger.LogError(ex.StackTrace, nameof(DiscordCommandInterpreter<T>));
					}
				}
				else
					await channel.SendMessage("No such command");
			}
		}
	}
}
