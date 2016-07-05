using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Net;
using Newtonsoft.Json;
using com.LuminousVector.Karuta.Commands;

namespace com.LuminousVector.Karuta
{
	public class LightingCommand : Command
	{
		public string url = "http://192.168.1.140/api";
		private string user;

		public LightingCommand() : base("lights", "Controls lighting via phillips Lighting")
		{
			_default = Setup;
			//user = Karuta.registry.GetString("lightuser");
			RegisterKeyword("on", On);
			RegisterKeyword("off", Off);

			RegisterOption('h', Hue);
			RegisterOption('s', Saturation);
			RegisterOption('b', Brightness);
		}


		//Hue
		void Hue(string h)
		{

		}

		//Saturation
		void Saturation(string s)
		{
			int hue;
			if (int.TryParse(s, out hue))
			{
				hue = (Math.Abs(hue) > 254) ? 254 : Math.Abs(hue);
				SenRequest(url + "/" + user + "/lights/1/state", "{\"sat\":" + hue + "}", "PUT");
				SenRequest(url + "/" + user + "/lights/3/state", "{\"sat\":" + hue + "}", "PUT");
			}
		}

		//Brightness
		void Brightness(string b)
		{
			int brightness;
			if (int.TryParse(b, out brightness))
			{
				brightness = (Math.Abs(brightness) > 254) ? 254 : Math.Abs(brightness);
				SenRequest(url + "/" + user + "/lights/1/state", "{\"bri\":" + brightness + "}", "PUT");
				SenRequest(url + "/" + user + "/lights/3/state", "{\"bri\":" + brightness + "}", "PUT");
			}
		}

		//Setup
		void Setup()
		{

			if (user != null && user != "")
			{
				return;
			}
			string userData = SenRequest(url, "{\"devicetype\":\"karuta\"}", "POST");
			if (userData.Contains("error"))
				Karuta.Write("Press the link button and try again.");
			else
			{
				Karuta.registry.SetValue("lightUser", GetRespose(userData));
				user = GetRespose(userData);
				Karuta.Write("Lights have been setup");
			}
		}

		//Turns Lights on
		void On()
		{
			if (user == null)
			{
				Setup();
				return;
			}
			SenRequest(url + "/" + user + "/lights/1/state", "{\"on\":true}", "PUT");
			SenRequest(url + "/" + user + "/lights/3/state", "{\"on\":true}", "PUT");
		}

		//Turns Lights off
		void Off()
		{
			if (user == null)
			{
				Setup();
				return;
			}
			SenRequest(url + "/" + user + "/lights/1/state", "{\"on\":false}", "PUT");
			SenRequest(url + "/" + user + "/lights/3/state", "{\"on\":false}", "PUT");
		}

		private string SenRequest(string url, string data, string method)
		{
			try
			{
				return Utils.HttpRequest(url, data, method);
			}
			catch (Exception e)
			{
				Karuta.Write(e.Message);
				return null;
			}
		}

		private string GetRespose(string output)
		{
			int i1 = 25, i2 = 0;
			while(output[i1 + i2] != '"')
				i2++;
			return output.Substring(i1, i2);
		}
	}
}
