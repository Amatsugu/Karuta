using System;
using System.Collections.Generic;
using System.Net;

namespace LuminousVector.Karuta
{
	public static class Extensions
	{

		public static readonly DateTime minTime = new DateTime(1970, 1, 1);

		//Extend DateTime to allow conversion to Epoch time
		public static int ToEpoch(this DateTime time)
		{
			TimeSpan t = time - minTime;
			return (int)t.TotalSeconds;
		}


		//Extend Lists to allow obtaining a random item
		public static T GetRandom<T>(this IList<T> list)
		{
			if (list.Count == 0)
				throw new Exception("Empty List!");
			if (list.Count == 1)
				return list[0];
			return list[Karuta.random.Next(0, list.Count)];
		}

		private static WebClient cli = null;
		//Extend Random to allow true random powered by random.org
		public static int TrueRNGNext(this Random random, int minValue, int maxValue)
		{
			if (minValue == maxValue)
				return minValue;
			if(minValue > maxValue)
			{
				int tmp = maxValue;
				maxValue = minValue;
				minValue = maxValue;
			}
			if(cli == null)
				cli = new WebClient();
			try
			{
				var data = cli.DownloadString(
					$"http://www.random.org/integers/?num=1&min={minValue}&max={maxValue}&col=1&base=10&format=plain&rnd=new"
					);
				if (string.IsNullOrWhiteSpace(data))
				{
					Karuta.Write("No response from random.org, falling back to psudo random");
					return random.Next(minValue, maxValue);
				}

				int value = 0;
				if (int.TryParse(data, out value))
				{
					return Utils.Clamp(value, minValue, maxValue);
				}
				else
				{
					Karuta.Write("Uable to parse data from random.org, falling back to psudo random");
					return random.Next(minValue, maxValue);
				}
			}catch(Exception e)
			{
				Karuta.Write($"Unable to connect to random.org {e.Message}");
				return random.Next(minValue, maxValue);
			}
		}
	}
}
