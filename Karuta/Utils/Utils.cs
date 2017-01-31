using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.IO;
using Imgur.API.Endpoints.Impl;
using System.Threading.Tasks;

namespace LuminousVector.Utils
{
	class Utils
	{
		public static float Clamp(float value, float range) => Clamp(value, -range, range);

		public static float Clamp(float value, float min, float max) => (value > max) ? max : (value < min) ? min: value;

		public static int Clamp(int value, int range) => Clamp(value, -range, range);

		public static int Clamp(int value, int min, int max) => (value > max) ? max : (value < min) ? min : value;

		public static bool IsEven(int value) => (value / 2) == (value / 2f);

		public static bool IsEven(float value) => ((int)value / 2) == (value / 2f);

		public static float SmartRound(float n, float d) => ((int)(n * d)) / d;

		public static double SmartRound(double n, double d) => ((int)(n * d)) / d;

		public static string HttpRequest(string url, string data, string method)
		{
			HttpWebRequest getRequest = (HttpWebRequest)WebRequest.Create(url);
			getRequest.Method = method;
			getRequest.UserAgent = "Mozilla/5.0 (Windows NT 6.1) AppleWebKit/535.2 (KHTML, like Gecko) Chrome/15.0.874.121 Safari/535.2";
			getRequest.AllowWriteStreamBuffering = true;
			getRequest.ProtocolVersion = HttpVersion.Version11;
			getRequest.AllowAutoRedirect = true;
			getRequest.ContentType = "application/x-www-form-urlencoded";

			byte[] byteArray = Encoding.ASCII.GetBytes(data);
			getRequest.ContentLength = byteArray.Length;
			Stream newStream = getRequest.GetRequestStream(); //open connection
			newStream.Write(byteArray, 0, byteArray.Length); // Send the data.
			newStream.Close();
			HttpWebResponse response = (HttpWebResponse)getRequest.GetResponse();
			StreamReader reader = new StreamReader(response.GetResponseStream());
			string output = reader.ReadToEnd();
			reader.Close();
			reader.Dispose();
			response.Close();
			return output;
		}
	}
}
