using System;
using System.Collections.Generic;
using System.Net;
using Nancy;
using LuminousVector.Karuta.RinDB.Responses;
using LuminousVector.Karuta.RinDB.Models;
using System.Drawing;

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
		
		//Convert to Base36
		public static string ToBas36String(this long value)
		{
			// 32 is the worst cast buffer size for base 2 and int.MaxValue
			int i = 32;
			char[] baseChars = new char[]
			{
				'0','1','2','3','4','5','6','7','8','9',
				'A','B','C','D','E','F','G','H','I','J',
				'K','L','M','N','O','P','Q','R','S','T',
				'U','V','W','X','Y','Z','a','b','c','d',
				'e','f','g','h','i','j','k','l','m','n',
				'o','p','q','r','s','t','u','v','w','x'
			};

			char[] buffer = new char[i];
			int targetBase = baseChars.Length;

			do
			{
				buffer[--i] = baseChars[value % targetBase];
				value = value / targetBase;
			}
			while (value > 0);

			char[] result = new char[32 - i];
			Array.Copy(buffer, i, result, 0, 32 - i);

			return new string(result);
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

		public static Response FromByteArray(this IResponseFormatter formatter, byte[] body, string contentType = null)
		{
			return new ByteArrayResponse(body, contentType);
		}

		public static Response FromImage(this IResponseFormatter formatter, Image image, string contentType = "image/png")
		{
			return new ImageResponse(image, contentType);
		}

		public static Response FromImageModel(this IResponseFormatter formatter, ImageModel image)
		{
			return null;
		}
	}
}
