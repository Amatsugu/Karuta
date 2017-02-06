using System;
using System.Linq;
using System.Collections.Generic;
using System.Net;
using static LuminousVector.Karuta.Karuta;

namespace LuminousVector.Utils.Extensions
{
	public static class Extensions
	{
		public static readonly char[] BASE60_CHARS = new char[]
		{
			'0','1','2','3','4','5','6','7','8','9',
			'A','B','C','D','E','F','G','H','I','J',
			'K','L','M','N','O','P','Q','R','S','T',
			'U','V','W','X','Y','Z','a','b','c','d',
			'e','f','g','h','i','j','k','l','m','n',
			'o','p','q','r','s','t','u','v','w','x'
		};
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
			return list[RANDOM.Next(0, list.Count)];
		}

		//String to base 60
		public static string ToBase60(this string value) => value.ToLower().GetHashCode().ToBase60();

		//Int to base 60
		public static string ToBase60(this int value) => ((long)value).ToBase60();

		//Convert to Base60
		public static string ToBase60(this long value)
		{
			bool neg = false;
			if (value < 0)
			{
				value = -value;
				neg = true;
			}
			int i = 64;
			

			char[] buffer = new char[i];
			int targetBase = BASE60_CHARS.Length;

			do
			{
				buffer[--i] = BASE60_CHARS[value % targetBase];
				value = value / targetBase;
			}
			while (value > 0);

			char[] result = new char[64 - i];
			Array.Copy(buffer, i, result, 0, 64 - i);

			string output = new string(result);
			return (neg) ? $"-{output}" : output;
		}

		public static List<string> SplitPreserveGrouping(this string s, char deliminator = ' ', char group = '"')
		{
			List<string> args = new List<string>();
			args.AddRange(from arg in s.Split(' ') where !string.IsNullOrWhiteSpace(arg) select arg);
			for (int i = 0; i < args.Count; i++)
			{
				args[i] = args[i].Replace("'", "\"");
				if (args[i][0] == '\"' && args[i][args[i].Length - 1] == '\"')
					args[i] = args[i].Replace("\"", "");
			}
			if (!Utils.IsEven((from q in args where q.Contains("\"") select q).ToList().Count))
			{
				for(int i = args.Count - 1; i >= 0; i--)
				{
					if(args[i].Contains('"'))
					{
						args[i] = args[i].Replace("\"", "");
						break;
					}
				}
			}
			List<int> start = new List<int>(), size = new List<int>();
			for (int i = 0; i < args.Count; i++)
			{
				if (args[i].Contains("\""))
				{
					if (start.Count == size.Count)
						start.Add(i);
					else
						size.Add(i - start.Last() + 1);
				}
			}

			//Find and merge quoted text
			int offset = 0;
			foreach (int g in start)
			{
				int i = start.IndexOf(g);
				string quote = string.Join(" ", args.GetRange(g - offset, size[i])).Replace("\"", "");
				args.RemoveRange(g - offset, size[i]);
				args.Insert(g - offset, quote);
				offset += size[i] - 1;
			}
			return args;
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
					Write("No response from random.org, falling back to psudo random");
					return random.Next(minValue, maxValue);
				}

				int value = 0;
				if (int.TryParse(data, out value))
				{
					return Utils.Clamp(value, minValue, maxValue);
				}
				else
				{
					Write("Uable to parse data from random.org, falling back to psudo random");
					return random.Next(minValue, maxValue);
				}
			}catch(Exception e)
			{
				Write($"Unable to connect to random.org {e.Message}");
				return random.Next(minValue, maxValue);
			}
		}
	}
}
