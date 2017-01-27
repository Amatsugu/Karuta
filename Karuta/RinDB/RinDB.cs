using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Npgsql;
using LuminousVector.Karuta.RinDB.Async;
using LuminousVector.Karuta.RinDB.Models;

namespace LuminousVector.Karuta.RinDB
{
	public static class RinDB
	{
		public static string VIEW_LOCATION { get; set; } = "RinDB_web/";
		public static string BASE_DIR { get; } = Karuta.registry.GetValue<string>("baseDir");
		public static string THUMB_DIR { get; } = $"{BASE_DIR}/RinDB/thumbs";
		public static string CONNECTION_STRING { get { return $"Host={HOST};Username={_user};Password={_pass};Database={_db};Pooling=true"; } }

		public const string HOST = "karuta.luminousvector.com";

		private static string _user, _pass, _db;


		public static void Init(string user, string pass, string db)
		{
			_user = user;
			_pass = pass;
			_db = db;

		}

		public static ImageModel GetImage(string id)
		{
			using (NpgsqlConnection con = new NpgsqlConnection(CONNECTION_STRING))
			{
				con.Open();
				ImageModel img = null;
				NpgsqlCommand cmd = con.CreateCommand();
				cmd.CommandText = $"SELECT fileuri, name FROM images WHERE id='{Uri.EscapeDataString(id)}'";
				using (var reader = cmd.ExecuteReader())
				{
					if (!reader.HasRows)
						return null;
					while (reader.Read())
					{
						img = new ImageModel()
						{
							fileUri = $@"{BASE_DIR}/{Uri.UnescapeDataString(reader.GetString(0))}",
							name = Uri.UnescapeDataString(reader.GetString(1)),
							id = Uri.EscapeDataString(id),
							tags = GetTags(id)
						};
					}
				}

				return img;
			}
		}


		public static int AddTagToAll(string tagid)
		{
			List<ImageModel> images = new List<ImageModel>();
			using (NpgsqlConnection con = new NpgsqlConnection(CONNECTION_STRING))
			{
				con.Open();
				NpgsqlCommand cmd = con.CreateCommand();
				cmd.CommandText = $"INSERT INTO tagmap(id, tagid, imageid) SELECT '{tagid}' || images.id, '{tagid}', images.id FROM images; ";
				return cmd.ExecuteNonQuery();
			}
		}

		public static ImageModel[] GetImages()
		{
			List<ImageModel> images = new List<ImageModel>();
			using (NpgsqlConnection con = new NpgsqlConnection(CONNECTION_STRING))
			{
				con.Open();
				NpgsqlCommand cmd = con.CreateCommand();
				cmd.CommandText = $"SELECT * FROM images";
				using (var reader = cmd.ExecuteReader())
				{
					if (!reader.HasRows)
						return images.ToArray();
					while (reader.Read())
					{
						images.Add(new ImageModel()
						{
							fileUri = $@"{BASE_DIR}/{Uri.UnescapeDataString(reader.GetString(4))}",
							name = Uri.UnescapeDataString(reader.GetString(3)),
							id = Uri.EscapeDataString(reader.GetString(0)),
							tags = GetTags(reader.GetString(0)),
							isnsfw = reader.GetBoolean(5)
						});
					}
				}

			}
			return images.ToArray();
		}

		public static ImageModel AddImage(ImageModel model)
		{
			using (NpgsqlConnection con = new NpgsqlConnection(CONNECTION_STRING))
			{
				con.Open();
				using (NpgsqlCommand cmd = con.CreateCommand())
				{
					cmd.CommandText = "SELECT nextval('images_index_seq');";
					model.id = $"{cmd.ExecuteScalar()}{model.timeadded}".ToBase60();
					cmd.CommandText = $"INSERT INTO images (id, name, timeadded, fileuri, isnsfw) VALUES('{model.id}', '{model.name}', '{model.timeadded}', '{model.fileUri}', '{model.isnsfw}')";
					cmd.ExecuteNonQuery();
					if (model.tags != null)
					{
						foreach (TagModel tag in model.tags)
						{
							AddTag(model.id, tag.id);
						}
					}
				}
			}
			model.fileUri = $@"{BASE_DIR}/{Uri.UnescapeDataString(model.fileUri)}";
			ThumbGenerator.QueueThumb(model);
			return model;
		}

		public static List<ImageModel> GetLatest(int count, int page = 1)
		{
			using (NpgsqlConnection con = new NpgsqlConnection(CONNECTION_STRING))
			{
				con.Open();
				List<ImageModel> images = new List<ImageModel>();
				using (NpgsqlCommand cmd = con.CreateCommand())
				{
					cmd.CommandText = $"SELECT id, fileuri, name FROM images WHERE isnsfw=false ORDER BY timeadded DESC";
					using (var reader = cmd.ExecuteReader())
					{
						int i = 0;
						if (!reader.HasRows)
							return null;
						while (reader.Read() && i++ < count * page)
						{
							if (i <= (page - 1) * count)
								continue;
							images.Add(new ImageModel()
							{
								id = Uri.UnescapeDataString(reader.GetString(0)),
								fileUri = $@"{BASE_DIR}/{Uri.UnescapeDataString(reader.GetString(1))}",
								name = Uri.UnescapeDataString(reader.GetString(2))
							});
						}
					}
					return images;
				}
			}
		}

		public static List<ImageModel> GetAll()
		{
			using (NpgsqlConnection con = new NpgsqlConnection(CONNECTION_STRING))
			{
				con.Open();
				List<ImageModel> images = new List<ImageModel>();
				using (NpgsqlCommand cmd = con.CreateCommand())
				{
					cmd.CommandText = $"SELECT id, fileuri, name FROM images";
					using (var reader = cmd.ExecuteReader())
					{
						if (!reader.HasRows)
							return null;
						while (reader.Read())
						{
							images.Add(new ImageModel()
							{
								id = Uri.UnescapeDataString(reader.GetString(0)),
								fileUri = $@"{BASE_DIR}/{Uri.UnescapeDataString(reader.GetString(1))}",
								name = Uri.UnescapeDataString(reader.GetString(2))
							});
						}
					}
					return images;
				}
			}
		}

		enum NSFWMode
		{
			None, Only, Allow, Default
		}

		public static List<ImageModel> GetSearch(string query, int count, int page = 1)
		{
			List<string> splitQuery = query.ToLower().Replace(':', ' ').SplitPreserveGrouping();
			List<string> tags = new List<string>();
			string cleanQuery = "";
			NSFWMode mode = NSFWMode.Allow;
			for(int i = 0; i < splitQuery.Count; i++)
			{
				switch (splitQuery[i])
				{
					case "tag":
						if (i + 1 > splitQuery.Count - 1)
							continue;
						tags.Add(splitQuery[i + 1]);

						i++;
						break;
					case "nsfw":
						if (i + 1 > splitQuery.Count - 1)
							continue;
						switch (splitQuery[i + 1].ToLower())
						{
							case "none":
								mode = NSFWMode.None;
								break;
							case "only":
								mode = NSFWMode.Only;
								break;
							case "allow":
								mode = NSFWMode.Allow;
								break;
							default:
								mode = NSFWMode.Default;
								break;
						}
						i++;
						break;
					default:
						if (cleanQuery == "")
							cleanQuery += splitQuery[i];
						else
							cleanQuery += $" {splitQuery[i]}";
						break;

				}
			}

			if (!string.IsNullOrWhiteSpace(cleanQuery))
				cleanQuery = $"lower(name) LIKE '%{Uri.EscapeDataString(cleanQuery)}%'";
			string tagQuery = "";
			foreach (string tag in tags)
			{
				if (tagQuery == "")
					tagQuery += $"T.id = '{tag.ToBase60()}' || I.id";
				else
					tagQuery += $" AND T.id = '{tag.ToBase60()}' || I.id";
			}
			if(tagQuery != "")
				cleanQuery += $"{(string.IsNullOrWhiteSpace(cleanQuery) ? "" : "AND")} EXISTS (SELECT T.id from tagmap T WHERE {tagQuery})";
			if (string.IsNullOrWhiteSpace(cleanQuery))
			{
				cleanQuery = ((mode == NSFWMode.Only) ? $"I.isnsfw = true" : ((mode == NSFWMode.None) ? $"I.isnsfw = false" : cleanQuery));
				if (mode == NSFWMode.Default)
					return new List<ImageModel>();
			}else
				cleanQuery = ((mode == NSFWMode.Only) ? $"I.isnsfw = true AND {cleanQuery}" : ((mode == NSFWMode.None) ? $"I.isnsfw = false AND {cleanQuery}" : cleanQuery));

			using (NpgsqlConnection con = new NpgsqlConnection(CONNECTION_STRING))
			{
				con.Open();
				List<ImageModel> images = new List<ImageModel>();
				using (NpgsqlCommand cmd = con.CreateCommand())
				{
					cmd.CommandText = $"SELECT I.id, I.fileUri, I.name, I.isnsfw, I.timeadded FROM images I {(string.IsNullOrWhiteSpace(cleanQuery) ? "" : $"WHERE {cleanQuery}")} ORDER BY I.timeadded DESC";
					using (var reader = cmd.ExecuteReader())
					{
						if (!reader.HasRows)
							return images;
						int i = 0;
						while (reader.Read() && i++ < count * page)
						{
							if (i <= (page - 1) * count)
								continue;
							images.Add(new ImageModel()
							{
								id = Uri.UnescapeDataString(reader.GetString(0)),
								fileUri = $@"{BASE_DIR}/{Uri.UnescapeDataString(reader.GetString(1))}",
								name = Uri.UnescapeDataString(reader.GetString(2))
							});
						}
					}
					return images;
				}
			}
		}

		public static TagModel[] GetTags(string imageId)
		{
			List<TagModel> tags = new List<TagModel>();
			using (NpgsqlConnection con = new NpgsqlConnection(CONNECTION_STRING))
			{
				con.Open();
				using (NpgsqlCommand cmd = con.CreateCommand())
				{
					cmd.CommandText = $"SELECT tagid FROM tagmap WHERE imageid='{Uri.EscapeDataString(imageId)}';";
					using (var reader = cmd.ExecuteReader())
					{
						if (!reader.HasRows)
							return tags.ToArray();
						while (reader.Read())
						{
							tags.Add(GetTag(reader.GetString(0)));
						}
					}
				}
			}
			return tags.ToArray();
		}

		public static TagModel GetTag(string tagID)
		{
			TagModel tag = null;
			using (NpgsqlConnection con = new NpgsqlConnection(CONNECTION_STRING))
			{
				con.Open();
				using (NpgsqlCommand cmd = con.CreateCommand())
				{
					cmd.CommandText = $"SELECT * FROM tags WHERE id='{Uri.EscapeDataString(tagID)}';";
					using (var reader = cmd.ExecuteReader())
					{
						if (!reader.HasRows)
							return null;
						while (reader.Read())
						{
							tag = new TagModel(Uri.UnescapeDataString(reader.GetString(1)))
							{
								type = reader.GetString(2),
								description = Uri.UnescapeDataString(reader.GetString(3)),
								parentID = reader.GetString(4)
							};
						}
					}
				}
			}
			return tag;
		}

		public static TagModel FindTag(string tagName)
		{
			TagModel tag = null;
			using (NpgsqlConnection con = new NpgsqlConnection(CONNECTION_STRING))
			{
				con.Open();
				using (NpgsqlCommand cmd = con.CreateCommand())
				{
					cmd.CommandText = $"SELECT * FROM tags WHERE name='{Uri.EscapeDataString(tagName)}';";
					using (var reader = cmd.ExecuteReader())
					{
						if (!reader.HasRows)
							return null;
						while (reader.Read())
						{
							tag = new TagModel(Uri.UnescapeDataString(reader.GetString(1)))
							{
								type = reader.GetString(2),
								description = Uri.UnescapeDataString(reader.GetString(3)),
								parentID = reader.GetString(4)
							};
						}
					}
				}
			}
			return tag;
		}

		public static TagModel CreateTag(TagModel tag)
		{
			using (NpgsqlConnection con = new NpgsqlConnection(CONNECTION_STRING))
			{
				con.Open();
				using (NpgsqlCommand cmd = con.CreateCommand())
				{
					try
					{
						cmd.CommandText = $"INSERT INTO tags(id, name, type, description, parentid) VALUES('{tag.id}', '{Uri.EscapeDataString(tag.name)}', '{tag.type}', '{Uri.EscapeDataString(tag.description)}', '{tag.parentID}');";
						cmd.ExecuteNonQuery();
					}catch
					{
						return GetTag(tag.id);
					}
				}
			}
			return tag;
		}

		public static void AddTag(string imageid, string tagid)
		{
			using (NpgsqlConnection con = new NpgsqlConnection(CONNECTION_STRING))
			{
				con.Open();
				using (NpgsqlCommand cmd = con.CreateCommand())
				{
					try
					{
						cmd.CommandText = $"INSERT INTO tagmap (id, imageid, tagid) VALUES('{tagid}{imageid}', '{imageid}', '{tagid}');";
						cmd.ExecuteNonQuery();
					}catch(Exception e)
					{
						Karuta.Write(e.Message);
					}
				}
			}
		}

		public static bool HasTag(string imageid, string tagid)
		{
			using (NpgsqlConnection con = new NpgsqlConnection(CONNECTION_STRING))
			{
				con.Open();
				using (NpgsqlCommand cmd = con.CreateCommand())
				{
					cmd.CommandText = $"SELECT EXISTS (SELECT id FROM tagmap WHERE id='{tagid}{imageid}');";
					return (bool)cmd.ExecuteScalar();
				}
			}
		}

		public static bool TagExists(string tag)
		{
			using (NpgsqlConnection con = new NpgsqlConnection(CONNECTION_STRING))
			{
				con.Open();
				using (NpgsqlCommand cmd = con.CreateCommand())
				{
					cmd.CommandText = $"SELECT EXISTS (SELECT name FROM tags WHERE name='{tag}');";
					return (bool)cmd.ExecuteScalar();
				}
			}
		}
	}
}
