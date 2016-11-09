using System;
using System.Collections.Generic;
using System.Linq;
using Discord;
using System.Text;
using System.Threading.Tasks;

namespace LuminousVector.Karuta.Commands.DiscordBot
{
	class DiscordEventCommand : DiscordCommand
	{
		private List<DiscordEvent> _events; 
		private string eName, eMode, eTime, eMessage, eCommand, eRate, eServer;

		public DiscordEventCommand(DiscordBot bot) : base("events", "Add/Remove/View upcoming events")
		{
			_bot = bot;
			Karuta.StartTimer("DiscordEventKeeper", CheckEvents, 0, 60 * 1000);
			_events = new List<DiscordEvent>();

			RegisterKeyword("all", ListEvents, "lists all events");
			RegisterKeyword("add", AddEvent, "adds a new event, options [n]ame, [t]ime, and either [e]vent message or [c]ommand are required. Optional: [r]epeat rate, repeat [m]ode, [s]ervers");
			RegisterKeyword("register", null, "register an exsisting event to be triggered on this channel on this server");
			RegisterOption('n', n => eName = n, "Specify the name of an event");
			RegisterOption('t', t => eTime = t, "Specify the time of an event, format: MM/DD/YYYY HH:MM");
			RegisterOption('m', m => eMode = m, "Specify the mode of an event, Valid modes are: none, daily, weekly, monthly, and anually");
			RegisterOption('e', e => eMessage = e, "Specify the message to be displayed when the even occurs, supports markdown formatting. Must be enclosed with quotes, can be used allongside the [c]ommand option");
			RegisterOption('c', c => eCommand = c, "Specify the name of an iamge command that will be executed when the event occurs, can be used allongside the [e]vent message option");
			RegisterOption('r', r => eRate = r, "Specify the rate multipler at which events will repeat, for example a value of 2 on an event of mode daily will repeat once every 2 days");
		}

		public override DiscordCommand Parse(List<string> args, Channel channel)
		{
			channel.SendMessage("This command is not yet implemented");
			return this;
		}

		async void Default()
		{
			string output = "Events in the next 7 days: \n";
			foreach(DiscordEvent e in from ev in _events where (ev.time - DateTime.Now).TotalDays <= 7 select ev)
			{
				output += $"{e.ToString()} \n";
			}
			await _channel.SendMessage(output);
			eRate = eTime = eName = eMessage = eCommand = eMode = null;
		}

		async void ListEvents()
		{
			string output = "Events in the next 7 days: \n";
			foreach (DiscordEvent e in _events)
			{
				output += $"{e.ToString()} \n";
			}
			await _channel.SendMessage(output);
			eRate = eTime = eName = eMessage = eCommand = eMode = null;
		}

		async void RegisterEvent()
		{

			await _channel.SendMessage("Event Registered to this channel");
		}

		async void AddEvent()
		{
			if(!string.IsNullOrEmpty(eName) && !string.IsNullOrEmpty(eMode) && !string.IsNullOrEmpty(eTime) && (!string.IsNullOrEmpty(eMessage) || !string.IsNullOrEmpty(eCommand)))
			{
				int rate = 0;
				if (!string.IsNullOrEmpty(eRate))
					int.TryParse(eRate, out rate);
				RepeatMode mode;
				switch (eMode.ToLower())
				{
					case "daily":
						mode = RepeatMode.Daily;
						break;
					case "weekly":
						mode = RepeatMode.Weekly;
						break;
					case "monthly":
						mode = RepeatMode.Monthly;
						break;
					case "anually":
						mode = RepeatMode.Anually;
						break;
					default:
						mode = RepeatMode.None;
						break;
				}
				DateTime eventTime = DateTime.Parse(eTime);
				if (!_bot.interpreter.IsImageCommand(eCommand))
				{
					await _channel.SendMessage($"This command '{eCommand}' cannot be executed by the EventBot");
					return;
				}
				_events.Add(new DiscordEvent(eName, mode, eventTime, async channels =>
				{
					DiscordClient client = _bot.client;
					string message = eName;
					if (!string.IsNullOrEmpty(eMessage))
					{
						message = eMessage;
					}
					foreach (ulong c in channels)
					{
						Channel chan = client.GetChannel(c);
						await chan.SendMessage(message);
						if(!string.IsNullOrWhiteSpace(eCommand))
						{
							_bot.InvokeCommand(eCommand, chan);
						}
					}
				}, _channel.Id, rate).RegisterChannel(_channel.Id));
				await _channel.SendMessage("Event Added!");


			}else
			{
				await _channel.SendMessage((from k in keywords where k == "add" select k.usage).First());
			}


			//Reset all options
			eRate = eTime = eName = eMessage = eCommand = eMode = null;
		}

		void CheckEvents(object info)
		{
			DateTime now = DateTime.Now;
			foreach(DiscordEvent e in from @event in _events where (@event.nextOccurance - now).TotalDays <= 1 select @event)
			{
				DateTime then = e.nextOccurance;
				if(then.Day == now.Day && then.Hour <= now.Hour)
				{
					e.ExecuteEvent();
				}
			}
		}


	}
}
