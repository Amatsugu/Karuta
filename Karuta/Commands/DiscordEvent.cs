using System;
using ProtoBuf;
using Discord;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LuminousVector.Karuta.Commands.DiscordBot
{
	public enum RepeatMode
	{
		None,
		Daily,
		Weekly,
		Monthly,
		Anually
	}

	[ProtoContract]
	class DiscordEvent
	{
		[ProtoMember(1)]
		public string name { get; }
		[ProtoMember(2)]
		public RepeatMode repeatMode { get; }
		[ProtoMember(3)]
		public DateTime time { get; }
		[ProtoMember(4)]
		public DateTime nextOccurance
		{
			get
			{
				if(_hasPassed)
					CalculateNextOccurance();
				return _nextOccurance;
			}
		}
		[ProtoMember(5)]
		public int repeatRate;
		[ProtoMember(6)]
		public List<ulong> channels;

		public bool isDone = false;

		private DateTime _nextOccurance;
		private Action<List<ulong>> _event;
		private bool _hasPassed = false;

		DiscordEvent()
		{
			CalculateNextOccurance();
		}

		public DiscordEvent(string name, RepeatMode repeatMode, DateTime time, Action<List<ulong>> eventContent, ulong channelID, int repeatRate = 1)
		{
			this.name = name;
			this.repeatMode = repeatMode;
			this.time = time;
			this.repeatRate = repeatRate;
			channels = new List<ulong>();
			channels.Add(channelID);
			_event = eventContent;
			_nextOccurance = DateTime.Now;
			CalculateNextOccurance();
		}

		public void ExecuteEvent()
		{
			_hasPassed = true;
			_event?.Invoke(channels);
		}

		void CalculateNextOccurance()
		{
			if(repeatMode == RepeatMode.None && time < DateTime.Now && _hasPassed)
			{
				isDone = true;
				return;
			}
			if (time > DateTime.Now)
			{
				_nextOccurance = time;
				_hasPassed = false;
				return;
			}
			while(_nextOccurance < DateTime.Now)
			{
				switch(repeatMode)
				{
					case RepeatMode.Daily:
						_nextOccurance = _nextOccurance.AddDays(1 * repeatRate);
						break;
					case RepeatMode.Weekly:
						_nextOccurance = _nextOccurance.AddDays(7 * repeatRate);
						break;
					case RepeatMode.Monthly:
						_nextOccurance = _nextOccurance.AddMonths(1 * repeatRate);
						break;
					case RepeatMode.Anually:
						_nextOccurance = _nextOccurance.AddYears(1 * repeatRate);
						break;
				}
			}
			_hasPassed = false;
		}

		public override string ToString()
		{
			return $"{name} on {time.ToLongDateString()} at {time.ToShortTimeString()}, {(repeatMode != RepeatMode.None ? $"{(repeatRate == 0 ? $"repeats {repeatMode}" : $"every {repeatRate} {ToRootName(repeatMode)}")}" : "")}";
		}

		string ToRootName(RepeatMode mode)
		{
			switch (mode)
			{
				case RepeatMode.Anually:
					return "year";
				case RepeatMode.Daily:
					return "Day";
				default:
					return "";
			}
		}

	}
}
