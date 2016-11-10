using System;
using System.Threading;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Nancy.Hosting.Self;
using Npgsql;
using LuminousVector.Karuta.Commands;
using LuminousVector.Karuta.RinDB.Models;

namespace LuminousVector.Karuta.RinDB
{
	[KarutaCommand]
	class RinDBCommand : Command
	{
		private bool _autoStart = false;
		private string _dbUser, _dbPass, _dbName;
		private NancyHost host;
		private string _buildDir, _buildNsfw, _buildTag, _tagInfo, _tagType = "tag";

		public RinDBCommand() : base("rindb", "RinDB server side")
		{
			_default = Default;

			bool? auto = Karuta.registry.GetBool("RinDB.autoStart");
			_autoStart = (auto == null) ? false : (auto == true) ? true : false;
			RegisterKeyword("stop", Stop, "stops the bot");
			RegisterKeyword("autostart", () =>
			{
				_autoStart = !_autoStart;
				Karuta.registry.SetValue("RinDB.autoStart", _autoStart);
				Karuta.Write($"Autostart {((_autoStart) ? "enabled" : "disabled")}");
			}, "enable/disable autostart");

			RegisterKeyword("rebuild", Rebuild, "Rebuild the database");
			RegisterKeyword("addTag", AddTag, "Add a tag to the database");

			RegisterOption('u', u => { _dbUser = u; Karuta.registry.SetValue("RinDB.user", u); }, "Sets the database user");
			RegisterOption('p', p => { _dbPass = p; Karuta.registry.SetValue("RinDB.pass", p); }, "Sets the database password");
			RegisterOption('d', d => { _dbName = d; Karuta.registry.SetValue("RinDB.name", d); }, "Sets the database name");
			RegisterOption('l', l => _buildDir = l, "Specify the directory to add to the database");
			RegisterOption('n', n => _buildNsfw = n.ToLower(), "Specify is the current build is nsfw");
			RegisterOption('t', t => _buildTag = t, "Specify a tag");
			RegisterOption('i', i => _tagInfo = i, "Specfiy tag info");
			RegisterOption('y', y => _tagType = y.ToLower(), "Specfiy tage type; tag | character | artist | work | meta");

			_dbUser = Karuta.registry.GetString("RinDB.user");
			_dbPass = Karuta.registry.GetString("RinDB.pass");
			_dbName = Karuta.registry.GetString("RinDB.name");

			init = () =>
			{
				if (_autoStart)
					_default();
			};
			//RinDB.CreateTag(new TagModel("Tohsaka Rin", "character"));
		}

		void AddTag()
		{
			if (string.IsNullOrWhiteSpace(_buildTag))
				throw new CommandInterpretorExeception("No Tag provided");
			if (!(_tagType == "tag" || _tagType == "character" || _tagType == "artist" || _tagType == "work" || _tagType == "meta"))
				throw new CommandInterpretorExeception("Invalid tag type provided");
			RinDB.CreateTag(new TagModel(_buildTag, _tagType, _tagInfo));
			_buildTag = _tagInfo = null;
			_tagType = "tag";
		}

		void Rebuild()
		{
			DateTime start = DateTime.Now;
			NpgsqlConnection con = new NpgsqlConnection(RinDB.CONNECTION_STRING);
			con.Open();
			NpgsqlCommand cmd = con.CreateCommand();
			cmd.CommandText = "DELETE FROM images; DELETE FROM tagmap; SELECT setval('images_index_seq', 1);";
			TagModel tag = RinDB.CreateTag(new TagModel("Tohsaka Rin", "character", "Rin Tohsaka from the Fate series by TYPE-MOON"));
			//RinDB.AddTagToAll(tag.id);
			return;
			cmd.ExecuteNonQuery();
			string[] dir = (from d in Directory.GetFiles($@"{RinDB.BASE_DIR}/OneTrueTohsaka", "*", SearchOption.AllDirectories) orderby d descending select d).ToArray();
			foreach (string f in dir)
			{
				cmd.CommandText = "SELECT nextval('images_index_seq')";
				string file = f.Replace("\\", "/");
				file = file.Replace($@"{RinDB.BASE_DIR}/", "");
				string name = Path.GetFileNameWithoutExtension(file);
				long epoch = long.Parse(name.Substring(1, name.IndexOf(']') - 1));
				name = name.Remove(0, epoch.ToString().Length + 3);
				name = Uri.EscapeDataString(name);
				string id = long.Parse($"{cmd.ExecuteScalar()}{epoch}").ToBas36String();
				cmd.CommandText = $"INSERT INTO images (id, fileuri, timeadded, name, isnsfw) VALUES('{id}', '{Uri.EscapeDataString(file)}', '{epoch}', '{name}', '{file.Contains("NSFW")}'); INSERT INTO tagmap VALUES('{tag.id}{id}','{tag.id}', '{id}');";
				cmd.ExecuteNonQuery();
			}
			cmd.Dispose();
			con.Close();
			Karuta.Write($"Rebuild Complete in {(DateTime.Now - start).TotalMilliseconds}ms");
		}

		void Build()
		{
			if (string.IsNullOrWhiteSpace(_buildDir))
				return;
			bool nsfw = (_buildNsfw == "true") ? true : false;
			if (!Directory.Exists($"{RinDB.BASE_DIR}/{_buildDir}"))
				return;
			using (NpgsqlConnection con = new NpgsqlConnection($"Host={RinDB.HOST};Username={_dbUser};Password={_dbPass};Database={_dbName}"))
			{
				con.Open();
				using (NpgsqlCommand cmd = con.CreateCommand())
				{
					string[] files = (from d in Directory.GetFiles($@"{RinDB.BASE_DIR}/{_buildDir}") orderby d descending select d).ToArray();

					foreach (string f in files)
					{
						cmd.CommandText = "SELECT nextval('images_index_seq')";
						string file = f.Replace("\\", "/");
						file = file.Replace($@"{RinDB.BASE_DIR}/", "");
						long curVal = (long)cmd.ExecuteScalar();
						string name = Path.GetFileNameWithoutExtension(file);
						string epoch = name.Substring(1, name.IndexOf(']') - 1);
						name = name.Remove(0, epoch.Length + 3);
						name = Uri.EscapeDataString(Path.GetFileNameWithoutExtension(name));
						string id = long.Parse($"{curVal}{epoch}").ToBas36String();
						cmd.CommandText = $"INSERT INTO images (id, fileuri, timeadded, name, nsfw) VALUES('{id}', '{Uri.EscapeDataString(file)}', '{epoch}', '{name}', '{nsfw}');";
						cmd.ExecuteNonQuery();
						if(!string.IsNullOrWhiteSpace(_buildTag))
						{
							string tagid = RinDB.FindTag(_buildTag).id;
							cmd.CommandText = $"INSERT INTO tagmap ('{tagid}{id}', '{tagid}', '{id}');";
							cmd.ExecuteNonQuery();
						}
					}
				}
			}
			_buildDir = _buildNsfw = null;
			_buildTag = "untagged";
			Karuta.Write("Build Complete");
		}

		void Default()
		{
			RinDB.Init(_dbUser, _dbPass, _dbName);
			Thread t = Karuta.CreateThread("RinDB", () =>
			{
				host = new NancyHost(new HostConfiguration() { UrlReservations = new UrlReservations() { CreateAutomatically = true } }, new Uri("http://localhost:1234"));
				host.Start();	
			});
		}

		public override void Stop()
		{
			RinDB.Close();
			NpgsqlConnection.ClearAllPools();
			host.Stop();
			host.Dispose();
		}
	}
}
