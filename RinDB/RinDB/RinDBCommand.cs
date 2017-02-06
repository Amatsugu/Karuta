using System;
using System.Threading;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Nancy.Hosting.Self;
using Npgsql;
using static LuminousVector.Karuta.Karuta;
using LuminousVector.Karuta.Commands;
using LuminousVector.RinDB.Models;
using LuminousVector.RinDB.Async;
using LuminousVector.Karuta;
using LuminousVector.Utils.Extensions;

namespace LuminousVector.RinDB
{
	[KarutaCommand]
	class RinDBCommand : Command, IDisposable
	{
		private bool _autoStart = false;
		private string _dbUser, _dbPass, _dbName;
		private NancyHost host;
		private string _buildDir, _buildNsfw, _buildTag, _tagInfo, _tagType = "tag";

		public RinDBCommand() : base("rindb", "RinDB server side")
		{
			_default = Default;

			bool? auto = REGISTY.GetBool("RinDB.autoStart");
			_autoStart = (auto == null) ? false : (auto == true) ? true : false;
			RegisterKeyword("stop", Stop, "stops the bot");
			RegisterKeyword("autostart", () =>
			{
				_autoStart = !_autoStart;
				REGISTY.SetValue("RinDB.autoStart", _autoStart);
				Write($"Autostart {((_autoStart) ? "enabled" : "disabled")}");
			}, "enable/disable autostart");

			RegisterKeyword("rebuild", Rebuild, "Rebuild the database");
			RegisterKeyword("addTag", AddTag, "Add a tag to the database");
			RegisterKeyword("build", Build, "Add the files in a specified folder to the database");
			RegisterKeyword("genThumbs", GenerateThumbs, "Generate thumbnails");

			RegisterOption('u', u => { _dbUser = u; REGISTY.SetValue("RinDB.user", u); }, "Sets the database user");
			RegisterOption('p', p => { _dbPass = p; REGISTY.SetValue("RinDB.pass", p); }, "Sets the database password");
			RegisterOption('d', d => { _dbName = d; REGISTY.SetValue("RinDB.name", d); }, "Sets the database name");
			RegisterOption('l', l => _buildDir = l, "Specify the directory to add to the database");
			RegisterOption('n', n => _buildNsfw = n.ToLower(), "Specify is the current build is nsfw");
			RegisterOption('t', t => _buildTag = t, "Specify a tag");
			RegisterOption('i', i => _tagInfo = i, "Specfiy tag info");
			RegisterOption('y', y => _tagType = y.ToLower(), "Specfiy tage type; tag | character | artist | work | meta");

			_dbUser = REGISTY.GetString("RinDB.user");
			_dbPass = REGISTY.GetString("RinDB.pass");
			_dbName = REGISTY.GetString("RinDB.name");

			init = () =>
			{
				if (_autoStart)
					_default();
			};
		}

		void AddTag()
		{
			if (string.IsNullOrWhiteSpace(_buildTag))
				throw new CommandInterpreterExeception("No Tag provided");
			if (!(_tagType == "tag" || _tagType == "character" || _tagType == "artist" || _tagType == "work" || _tagType == "meta"))
				throw new CommandInterpreterExeception("Invalid tag type provided");
			RinDB.CreateTag(new TagModel(_buildTag, _tagType, _tagInfo));
			_buildTag = _tagInfo = null;
			_tagType = "tag";
		}

		void GenerateThumbs()
		{
			CreateThread("RinDB.GenerateThumbs", () =>
			{
				ThumbGenerator.QueueThumb(RinDB.GetAll());
			});
		}

		void Rebuild()
		{
			CreateThread("RinDB.rebuild", () =>
			{
				Write("Rebuild Start");
				DateTime start = DateTime.Now;
				using (NpgsqlConnection con = RinDB.GetConnection())
				{
					con.Open();
					using (NpgsqlCommand cmd = con.CreateCommand())
					{
						cmd.CommandText = "DELETE FROM images; DELETE FROM tagmap; DELETE FROM tags; SELECT setval('images_index_seq', 1);";
						cmd.ExecuteNonQuery();
						//string[] files = Directory.GetFiles(RinDB.THUMB_DIR);
						//foreach (string f in files)
						//	File.Delete(f);
						//RinDB.AddTagToAll(tag.id);
						//return;
						RegisterTags();
						Dictionary<string, string[]> buildList = new Dictionary<string, string[]>();
						buildList.Add("OneTrueTohsaka", new string[] { "Tohsaka Rin", "Fate" });
						buildList.Add("awwnime", null);
						buildList.Add("Animewallpaper", null);
						buildList.Add("Animelegs", null);
						buildList.Add("OneTrueRem", new string[] { "Rem", "Re:Zero" });
						buildList.Add("ZettaiRyouiki", new string[] { "Zettai Ryouiki" });
						buildList.Add("ZettaiRyouikiIRL", new string[] { "Zettai Ryouiki", "IRL" });
						buildList.Add("consentacles", new string[] { "Tentacles", "Consentacles" });
						buildList.Add("grisaia", new string[] { "Grisaia" });
						buildList.Add("hatsune", new string[] { "Hatsune Miku" });
						buildList.Add("hentiny", null);
						buildList.Add("imouto", null);
						buildList.Add("kemonomimi", new string[] { "Kemonomimi" });
						buildList.Add("megane", new string[] { "Megane" });
						buildList.Add("nopan", new string[] { "No Pantsu" });
						buildList.Add("OneTrueBiribiri", new string[] { "Misaka Mikoto", "A Certain Scientific Railgun" });
						buildList.Add("OneTrueRam", new string[] { "Ram", "Re:Zero" });
						buildList.Add("OreGairuSNAFU", new string[] { "Ore Gairu SNAFU" });
						buildList.Add("plamemo", new string[] { "Plastic Memories" });
						buildList.Add("ReLIFE", new string[] { "ReLife" });
						buildList.Add("SpiceandWolf", new string[] { "Spice and Wolf" });
						buildList.Add("Tentai", new string[] { "Tentacles" });
						buildList.Add("twintails", new string[] { "Twin Tails" });
						buildList.Add("Megumin", new string[] { "Megumin", "KonoSuba" });
						buildList.Add("SukumizuIRL", new string[] { "Sukumizu", "IRL" });
						DoBuild(buildList);
					}
				}
				Write($"Rebuild Complete in {(DateTime.Now - start).Seconds}s");
			});
			//GenerateThumbs();
		}

		void DoBuild(Dictionary<string, string[]> buildList)
		{
			foreach (string item in buildList.Keys)
			{
				Write($"Building: {item}...", false);
				string[] dir = Directory.GetFiles($@"{RinDB.BASE_DIR}/{item}", "*", SearchOption.AllDirectories);
				foreach (string f in dir)
				{
					string file = f.Replace("\\", "/");
					file = file.Replace($@"{RinDB.BASE_DIR}/", "");
					string name = Path.GetFileNameWithoutExtension(file);
					long epoch = long.Parse(name.Substring(1, name.IndexOf(']') - 1));
					name = name.Remove(0, epoch.ToString().Length + 3);
					name = Uri.EscapeDataString(name);
					string id = file.ToBase60();

					DBUpdateQueue.QueueCommand($"INSERT INTO images (id, fileuri, timeadded, name, isnsfw) VALUES('{id}', '{Uri.EscapeDataString(file)}', '{epoch}', '{name}', '{file.Contains("NSFW")}');");
					//cmd.CommandText = $"INSERT INTO images (id, fileuri, timeadded, name, isnsfw) VALUES('{id}', '{Uri.EscapeDataString(file)}', '{epoch}', '{name}', '{file.Contains("NSFW")}');";
					if(buildList[item] != null)
					{
						foreach (string tag in buildList[item])
						{
							string tagID = tag.ToBase60();
							//cmd.CommandText += $"INSERT INTO tagmap VALUES('{tagID}{id}','{id}', '{tagID}');";
							DBUpdateQueue.QueueCommand($"INSERT INTO tagmap VALUES('{tagID}{id}','{id}', '{tagID}');");
						}
					}
					//cmd.ExecuteNonQuery();
					ThumbGenerator.QueueThumb(new ImageModel() { id = id, fileUri = $@"{RinDB.BASE_DIR}/{file}" });
				}
				Write(" Done!");
			}
		}

		void RegisterTags()
		{
			//Default Tags
			"Tohsaka Rin".ToBase60();
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
			RinDB.CreateTag(new TagModel("No Pantsu", "tag", "A character is not wearing pantsu"));
			RinDB.CreateTag(new TagModel("Sukumizu", "tag", "A variant of swimwear intended for use during swim lessons, predominately in Japan. They are traditionally a one piece and don't have any openings."));
		}

		void Build()
		{
			if (string.IsNullOrWhiteSpace(_buildDir))
				return;
			CreateThread($"RinDB.build_{_buildDir}", () =>
			{
				Write($"Building {_buildDir}");
				DateTime start = DateTime.Now;
				bool nsfw = (_buildNsfw == "true") ? true : false;
				if (!Directory.Exists($"{RinDB.BASE_DIR}/{_buildDir}"))
					return;
				Dictionary<string, string[]> buildList = new Dictionary<string, string[]>();
				buildList.Add(_buildDir, _buildTag.Split(','));
				DoBuild(buildList);
				Write($"Build Complete: {_buildDir}");
				Write($"Finished in {(DateTime.Now - start).Seconds}s");
				_buildDir = _buildNsfw = _buildTag = "";
			});
		}

		void Default()
		{
			RinDB.Init(_dbUser, _dbPass, _dbName);
			Thread t = CreateThread("RinDB", () =>
			{
				host = new NancyHost(new HostConfiguration() { UrlReservations = new UrlReservations() { CreateAutomatically = true } }, new Uri("http://localhost:1234"));
				host.Start();	
			});
		}

		public override void Stop()
		{
			Dispose();
		}

		public void Dispose()
		{
			ThumbGenerator.Close();
			DBUpdateQueue.Close();
			host?.Stop();
			host?.Dispose();
		}
	}
}
