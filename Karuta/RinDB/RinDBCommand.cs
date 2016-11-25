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
			RegisterKeyword("build", Build, "Add the files in a specified folder to the database");

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
			Karuta.CreateThread("RinDB.rebuild", () =>
			{
				Karuta.Write("Rebuild Start");
				DateTime start = DateTime.Now;
				NpgsqlConnection con = new NpgsqlConnection(RinDB.CONNECTION_STRING);
				con.Open();
				NpgsqlCommand cmd = con.CreateCommand();
				cmd.CommandText = "DELETE FROM images; DELETE FROM tagmap; DELETE FROM tags; SELECT setval('images_index_seq', 1);";
				string[] files = Directory.GetFiles(RinDB.THUMB_DIR);
				foreach (string f in files)
					File.Delete(f);
				cmd.ExecuteNonQuery();
				//RinDB.AddTagToAll(tag.id);
				//return;
				RegisterTags();
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
					string id = $"{cmd.ExecuteScalar()}{epoch}".ToBase60();
					cmd.CommandText = $"INSERT INTO images (id, fileuri, timeadded, name, isnsfw) VALUES('{id}', '{Uri.EscapeDataString(file)}', '{epoch}', '{name}', '{file.Contains("NSFW")}');";
					cmd.CommandText += $"INSERT INTO tagmap VALUES('{"Tohsaka Rin".ToBase60()}{id}','{id}', '{"Tohsaka Rin".ToBase60()}');";
					cmd.CommandText += $"INSERT INTO tagmap VALUES('{"Fate".ToBase60()}{id}','{id}', '{"Fate".ToBase60()}');";
					//Karuta.Write(cmd.CommandText);
					cmd.ExecuteNonQuery();
				}
				cmd.Dispose();
				con.Close();
				Karuta.Write($"Rebuild Complete in {(DateTime.Now - start).Seconds}s");
			});
		}

		void RegisterTags()
		{
			RinDB.CreateTag(new TagModel("Tohsaka Rin", "character", "Rin Tohsaka from the Fate series by TYPE-MOON"));
			RinDB.CreateTag(new TagModel("Hatsune Miku", "character", "Vocaloid Hatsune Miku"));
			RinDB.CreateTag(new TagModel("Zettai Ryouiki", "tag", "Absolute Territory"));
			RinDB.CreateTag(new TagModel("Spice and Wolf", "work", "Spice and wolf Anime/Manga/Lite Novel"));
			RinDB.CreateTag(new TagModel("Holo", "character", "The wise wolf of Yoitsu"));
			RinDB.CreateTag(new TagModel("Twin Tails", "tag", "Hair in twin tails"));
			RinDB.CreateTag(new TagModel("Tsundere", "tag", "Character is a tsundere, or shows tsundere traits"));
			RinDB.CreateTag(new TagModel("Rem", "character", "Rem from Re:Zero"));
			RinDB.CreateTag(new TagModel("Ram", "character", "Ram from Re:Zero"));
			RinDB.CreateTag(new TagModel("Emilia ", "character", "I love Emilia"));
			RinDB.CreateTag(new TagModel("Misaka Mikoto", "character", "Misaka Mikoto from A Certain Madigcal Index/A Certain Scientific Railgun"));
			RinDB.CreateTag(new TagModel("A Certain Scientific Railgun", "work", "A Certain Scientific Railgun from the Raildex series"));
			RinDB.CreateTag(new TagModel("A Certain Magical Index", "work", "A Certain Magical Index from the Raildex series"));
			TagModel fate = RinDB.CreateTag(new TagModel("Fate", "work", "The Fate series by TYPE-MOON"));
			RinDB.CreateTag(new TagModel("Fate/Zero", "work", "Fate/Zero from the Fate series by TYPE-MOON") { parentID = fate.id });
			RinDB.CreateTag(new TagModel("Fate/Stay Night", "work", "Fate/Stay Night from the Fate series by TYPE-MOON") { parentID = fate.id });
			RinDB.CreateTag(new TagModel("Fate/Stay Night: UBW", "work", "Fate/Stay Night: UBW from the Fate series by TYPE-MOON") { parentID = fate.id });
			RinDB.CreateTag(new TagModel("ReLife", "work", "The ReLife Anime/Manga"));
			RinDB.CreateTag(new TagModel("Megane", "tag", "Character wears glasses"));
			RinDB.CreateTag(new TagModel("Kemonomimi", "tag", "Character has animal ears and/or tail"));
			RinDB.CreateTag(new TagModel("Re:Zero", "work", "Re:Zero Anime/Manga"));
			TagModel irl = RinDB.CreateTag(new TagModel("IRL", "meta", "Image is of a real person"));
			TagModel tentacle = RinDB.CreateTag(new TagModel("Tentacles", "tag", "You know where this is going"));
			RinDB.CreateTag(new TagModel("Contentacles", "tag", "Consentual tentacles") { parentID = tentacle.id });
			RinDB.CreateTag(new TagModel("Cosplay", "tag", "A person is cosplaying a character") { parentID = irl.id });
			RinDB.CreateTag(new TagModel("Plastic Memories", "work", "Plastic Memories Anime"));
			RinDB.CreateTag(new TagModel("Grisaia", "work", "The Grisaia series of VNs and Anime"));
			RinDB.CreateTag(new TagModel("Ore Gairu SNAFU", "work", "Yahari Ore no Seishun Love Comedy wa Machigatteiru Anime/Manga."));
			RinDB.CreateTag(new TagModel("Megumin", "character", "Explosion!!!"));
			RinDB.CreateTag(new TagModel("KonoSuba", "work", "kono subarashii sekai ni shukufuku wo Anime/Manga"));
			RinDB.CreateTag(new TagModel("Pettanko", "tag", "Small/Flat breasted character but do not have a child-like appearance i.e: Holo"));
			RinDB.CreateTag(new TagModel("Loli", "tag", "Character who has a childish appearance, i.e Shinobu"));
		}

		void Build()
		{
			if (string.IsNullOrWhiteSpace(_buildDir))
				return;
			Karuta.CreateThread($"RinDB.build_{_buildDir}", () =>
			{
				Karuta.Write($"Building {_buildDir}");
				DateTime start = DateTime.Now;
				bool nsfw = (_buildNsfw == "true") ? true : false;
				if (!Directory.Exists($"{RinDB.BASE_DIR}/{_buildDir}"))
					return;
				using (NpgsqlConnection con = new NpgsqlConnection($"Host={RinDB.HOST};Username={_dbUser};Password={_dbPass};Database={_dbName}"))
				{
					con.Open();
					using (NpgsqlCommand cmd = con.CreateCommand())
					{
						string[] files = (from d in Directory.GetFiles($@"{RinDB.BASE_DIR}/{_buildDir}", "*", SearchOption.AllDirectories) orderby d descending select d).ToArray();
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
							string id = long.Parse($"{curVal}{epoch}").ToBase60();
							try
							{
								cmd.CommandText = $"INSERT INTO images (id, fileuri, timeadded, name, isnsfw) VALUES('{id}', '{Uri.EscapeDataString(file)}', '{epoch}', '{name}', '{file.Contains("NSFW")}');";
								cmd.ExecuteNonQuery();
							} catch
							{

							}
							if (!string.IsNullOrWhiteSpace(_buildTag))
							{
								string tag = _buildTag.ToBase60();
								cmd.CommandText = $"INSERT INTO tagmap VALUES('{tag}{id}', '{id}', '{tag}');";
								cmd.ExecuteNonQuery();
							}
						}
					}
				}
				Karuta.Write($"Build Complete: {_buildDir}");
				Karuta.Write($"Finished in {(DateTime.Now - start).Seconds}s");
				_buildDir = _buildNsfw = _buildTag = "";
			});
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
