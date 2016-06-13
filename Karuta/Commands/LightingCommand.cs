using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Net;
using Newtonsoft.Json;

namespace com.LuminousVector.Karuta
{
	public class LightingCommand : Command
	{
		public LightingCommand() : base("lights", "Controls lighting via phillips Lighting", "lights <on/off>") { }
		public string url = "http://192.168.1.140/api";
		private string user;

		public override void Run(string[] args)
		{
			user = Karuta.registry.GetString("lightUser");
			if(args.Length == 1)
			{
				if(user != null && user != "")
				{
					Karuta.Write("lights have already been set up");
					return;
				}
				string userData = SendCommand(url, "{\"devicetype\":\"karuta\"}", "POST");
				if (userData.Contains("error"))
					Karuta.Write("Press the link button and try again.");
				else
				{
					//Karuta.registry.SetValue("lightUser", GetRespose(userData));
					user = GetRespose(userData);
					Karuta.Write("Lights have been setup");
				}
			}
			else if (args.Length == 2)
			{
				if (user == null || user == "")
					Run(new string[] { "lights" });
				string state = ((args[1] == "on") ? "true" : "false");
				SendCommand(url + "/" + user + "/lights/1/state", "{\"on\":" + state + "}", "PUT");
				SendCommand(url + "/" + user + "/lights/3/state", "{\"on\":" + state + "}", "PUT");
			}
			else if (args.Length == 3)
			{
				if (GetIndexOfOption(args, 'b') != -1)
				{
					string value = GetValueOfOption(args, 'b');
					if (value == null)
					{
						Karuta.Write("Specify a brightness level");
					}
					else
					{
						int brightness;
						if (int.TryParse(value, out brightness))
						{
							brightness = (Math.Abs(brightness) > 254) ? 254 : Math.Abs(brightness);
							SendCommand(url + "/" + user + "/lights/1/state", "{\"bri\":" + brightness + "}", "PUT");
							SendCommand(url + "/" + user + "/lights/3/state", "{\"bri\":" + brightness + "}", "PUT");
						}
					}
				}else if (GetIndexOfOption(args, 's') != -1)
				{
					string value = GetValueOfOption(args, 's');
					if (value == null)
					{
						Karuta.Write("Specify a saturation level");
					}
					else
					{
						int hue;
						if (int.TryParse(value, out hue))
						{
							hue = (Math.Abs(hue) > 254) ? 254 : Math.Abs(hue);
							SendCommand(url + "/" + user + "/lights/1/state", "{\"sat\":" + hue + "}", "PUT");
							SendCommand(url + "/" + user + "/lights/3/state", "{\"sat\":" + hue + "}", "PUT");
						}
					}
				}

			}
		}

		private string SendCommand(string url, string data, string method)
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
