using System.Threading;
using System.Collections.Generic;
using com.LuminousVector.Karuta.Util;
using com.LuminousVector.Karuta.Commands;
using Q42.HueApi;
using System;

namespace com.LuminousVector.Karuta
{
	public class LightingCommand : Command
	{
		private string _url = "192.168.1.140";
		private LocalHueClient _client;
		private string _user;
		private Dictionary<string, Color> _lightColors = new Dictionary<string, Util.Color>();
		private Timer _colorKeeper;

		public LightingCommand() : base("lights", "Controls lighting via phillips Lighting")
		{
			_default = Setup;
			_user = Karuta.registry.GetString("lightuser");
			if(_user != "")
			{
				_client = new LocalHueClient(_url);
				_client.Initialize(_user);
			}
			LoadLightColors();
			RegisterKeyword("on", On);
			RegisterKeyword("off", Off);
			RegisterKeyword("getColors", Color);
			RegisterKeyword("keepColors", KeepColor);
			RegisterKeyword("stop", StopColorKeeper);
			RegisterKeyword("saveColors", SaveColor);

			RegisterOption('h', Hue);
			RegisterOption('s', Saturation);
			RegisterOption('b', Brightness);
		}

		private void StopColorKeeper()
		{
			_colorKeeper?.Dispose();
		}

		//Keep Colors
		private void KeepColor()
		{
			if(_colorKeeper != null)
			{
				Karuta.Write("Color Keeper is already running!");
				return;
			}
			if(_lightColors?.Count == 0)
			{
				Karuta.Write("No colors saved.");
				return;
			}
			bool lightsSet = false;
			_colorKeeper = new Timer(async info =>
			{
				//Karuta.Write("Checking Lights...");
				List<Light> lights = new List<Light>();
				lights.AddRange(await _client.GetLightsAsync());
				bool reachable = false;
				foreach(Light l in lights)
				{
					if (l.State.IsReachable == true)
						reachable = true;
				}
				if(reachable)
				{
					if (!lightsSet)
					{
						foreach (string l in _lightColors.Keys)
						{
							//Karuta.Write("Lights reachable!");
							LightCommand cmd = new LightCommand();
							Color c = _lightColors[l];
							cmd.Hue = c.h;
							cmd.Saturation = c.s;
							//cmd.Brightness = (byte)c.b;
							//Karuta.Write("Setting Colors: " + l);
							await _client.SendCommandAsync(cmd, new string[] { l });
							lightsSet = true;
						}
					}
				}else
				{
					lightsSet = false;
				}
			}, null, 0, 2000);
			
		}

		//Save current Colors
		private async void SaveColor()
		{
			if (_client == null)
				Setup();
			List<Light> lights = new List<Light>();
			lights.AddRange(await _client.GetLightsAsync());
			foreach(Light l in lights)
			{
				State state = l.State;
				if (state.IsReachable == true)
				{
					_lightColors.Add(l.Id, new Color((int)state.Hue, (int)state.Saturation, state.Brightness));
				}
			}
			string data = "";
			foreach(string s in _lightColors.Keys)
			{
				if (data != "")
					data += "|";
				data += s + "`" + _lightColors[s].ToString();
			}
			Karuta.registry.SetValue("lightColors", data);
			Karuta.Write("Colors Saved!");
		}

		//Load light colors
		void LoadLightColors()
		{
			string data = Karuta.registry.GetString("lightColors");
			if (data == "")
				return;
			string[] lights = data.Split('|');
			foreach(string l in lights)
			{
				string[] lSplit = l.Split('`');
				_lightColors.Add(lSplit[0], new Color(lSplit[1]));
			}
		}


		//Color
		async void Color()
		{
			if (_client == null)
				Setup();
			LightCommand cmd = new LightCommand();
			List<Light> lights = new List<Light>();
			lights.AddRange(await _client.GetLightsAsync());
			foreach(Light l in lights)
			{
				State state = l.State;
				if (state.IsReachable == false || state.IsReachable == null)
					continue;
				Karuta.Write("\nLight: " + l.Name +" H:" + state.Hue + " S:" + state.Saturation + " B:" + state.Brightness);
			}

		}

		//Hue
		async void Hue(string h)
		{
			if (_client == null)
				Setup();
		}

		//Saturation
		async void Saturation(string s)
		{
			if (_client == null)
				Setup();
		}

		//Brightness
		async void Brightness(string b)
		{
			if (_client == null)
				Setup();
		}

		//Setup
		async void Setup()
		{
			_client = new LocalHueClient(_url);
			if(_user == "")
				_user = await _client.RegisterAsync("Karuta.lighting", "Karuta");
			_client.Initialize(_user);
			List<Light> light = new List<Light>();
		}

		//Turns Lights on
		async void On()
		{
			if (_client == null)
				Setup();
			LightCommand cmd = new LightCommand();
			cmd.On = true;
			await _client.SendCommandAsync(cmd);
		}

		//Turns Lights off
		async void Off()
		{
			if (_client == null)
				Setup();
			LightCommand cmd = new LightCommand();
			cmd.On = false;
			await _client.SendCommandAsync(cmd);
		}
	}
}
