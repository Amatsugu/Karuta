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
		private Color _defaultColor = new Color(14910, 144, 254);
		private bool _autoStart = false;

		public LightingCommand() : base("lights", "Controls lighting via phillips Lighting")
		{
			_default = Setup;
			_user = Karuta.registry.GetString("lightuser");
			bool? auto = Karuta.registry.GetBool("lightAutostart");
			_autoStart = (auto == null) ? false : (auto == true) ? true : false;
			if (_user != "")
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
			RegisterKeyword("autostart", () =>
			{
				_autoStart = !_autoStart;
				Karuta.registry.SetValue("lightAutostart", _autoStart);
				Karuta.Write("Autostart " + ((_autoStart) ? "enabled" : "disabled"));
			}, "enable/disable autostart");

			RegisterOption('h', Hue);
			RegisterOption('s', Saturation);
			RegisterOption('b', Brightness);

			init = () =>
			{
				if (_autoStart)
					KeepColor();
			};
		}

		private void StopColorKeeper()
		{
			_colorKeeper?.Dispose();
			_colorKeeper = null;
		}

		//Keep Colors
		private void KeepColor()
		{
			if (_client == null)
				Setup();
			if(!_client.IsInitialized)
			{
				Karuta.Write("Not connected to lights.");
				return;
			}
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
			_colorKeeper = Karuta.StartTimer("Lights Color Keeper", async info =>
			{
				foreach(Light l in await _client.GetLightsAsync())
				{
					State state = l.State;
					if (state.Saturation == _defaultColor.s && state.Hue == _defaultColor.h && state.Brightness == (byte)_defaultColor.b)
					{
						Color curColor = _lightColors[l.Id];
						LightCommand cmd = new LightCommand();
						cmd.Hue = curColor.h;
						cmd.Saturation = curColor.s;
						await _client.SendCommandAsync(cmd, new string[] { l.Id });
					}
				}
			}, 0, 1000);
			
		}

		//Save current Colors
		private async void SaveColor()
		{
			if (_client == null)
				Setup();
			if (!_client.IsInitialized)
			{
				Karuta.Write("Not connected to lights.");
				return;
			}
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
			if (!_client.IsInitialized)
			{
				Karuta.Write("Not connected to lights.");
				return;
			}
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
#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously
		async void Hue(string h)
#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously
		{
			if (_client == null)
				Setup();
			if (!_client.IsInitialized)
			{
				Karuta.Write("Not connected to lights.");
				return;
			}
		}

		//Saturation
#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously
		async void Saturation(string s)
#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously
		{
			if (_client == null)
				Setup();
			if (!_client.IsInitialized)
			{
				Karuta.Write("Not connected to lights.");
				return;
			}
		}

		//Brightness
#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously
		async void Brightness(string b)
#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously
		{
			if (_client == null)
				Setup();
			if (!_client.IsInitialized)
			{
				Karuta.Write("Not connected to lights.");
				return;
			}
		}

		//Setup
		async void Setup()
		{
			_client = new LocalHueClient(_url);
			try
			{
				if(_user == "" || _user == null)
					_user = await _client.RegisterAsync("Karuta.lighting", "Karuta");
				else
					_client.Initialize(_user);
			}catch(Exception e)
			{
				Karuta.Write(e.Message);
				Karuta.Write(e.StackTrace);
			}
			Karuta.registry.SetValue("lightuser", _user);
			List<Light> light = new List<Light>();
		}

		//Turns Lights on
		async void On()
		{
			if (_client == null)
				Setup();
			if (!_client.IsInitialized)
			{
				Karuta.Write("Not connected to lights.");
				return;
			}
			LightCommand cmd = new LightCommand();
			cmd.On = true;
			await _client.SendCommandAsync(cmd);
		}

		//Turns Lights off
		async void Off()
		{
			if (_client == null)
				Setup();
			if (!_client.IsInitialized)
			{
				Karuta.Write("Not connected to lights.");
				return;
			}
			LightCommand cmd = new LightCommand();
			cmd.On = false;
			await _client.SendCommandAsync(cmd);
		}
	}
}
