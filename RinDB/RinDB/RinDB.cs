using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Npgsql;
using static LuminousVector.Karuta.Karuta;
using LuminousVector.Utils.Extensions;
using LuminousVector.RinDB.Async;
using LuminousVector.RinDB.Models;

namespace LuminousVector.RinDB
{
	public static class RinDB
	{
		public static string VIEW_LOCATION { get; set; } = "RinDB_Web/";
		public static string BASE_DIR { get; } = REGISTY.GetString("baseDir");
		public static string THUMB_DIR { get; } = $"{BASE_DIR}/RinDB/thumbs";


		private static string CONNECTION_STRING { get { return $"Host={HOST};Username={_user};Password={_pass};Database={_db};Pooling=true"; } }

		public const string HOST = "karuta.luminousvector.com";

		private static string _user, _pass, _db;

		enum NSFWMode
		{
			None, Only, Allow, Default
		}

		public static void Init(string user, string pass, string db)
		{
			_user = user;
			_pass = pass;
			_db = db;
			ThumbGenerator.Start();
			DBUpdateQueue.Start();
		}


		/// <summary>
		/// Returns a new DB connection
		/// </summary>
		/// <returns></returns>
		internal static NpgsqlConnection GetConnection() => new NpgsqlConnection(CONNECTION_STRING);


		/// <summary>
		/// Get an image with all it's info from the DB
		/// </summary>
		/// <param name="id">The id of the image</param>
		/// <returns>Completed ImageModel</returns>
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
							id = Uri.EscapeDataString(id)
						};
					}
				}

				return img;
			}
		}


		/// <summary>
		/// Add a specified tag to all images in the DB
		/// </summary>
		/// <param name="tagid">The TagModel of the tag to be added</param>
		/// <returns>Number of items affected</returns>
		public static int AddTagToAll(TagModel tag) => AddTagToAll(tag.id);

		/// <summary>
		/// Add a specified tag to all images in the DB
		/// </summary>
		/// <param name="tagid">The tag to be added</param>
		/// <returns>Number of items affected</returns>
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

		/// <summary>
		/// Add a new Image to the DB
		/// </summary>
		/// <param name="model">ImageModel of the image to be added</param>
		/// <returns>ImageModel unchanged</returns>
		public static ImageModel AddImage(ImageModel model)
		{
			if (ImageExists(model))
			{
				model.fileUri = $@"{BASE_DIR}/{Uri.UnescapeDataString(model.fileUri)}";
				return model;
			}
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

		/// <summary>
		/// Get images from the DB ordered by time added
		/// </summary>
		/// <param name="count">Number of images to get</param>
		/// <param name="page">Page to get</param>
		/// <returns>List of completed ImageModels</returns>
		public static List<ImageModel> GetLatest(int count, int page = 1)
		{
			using (NpgsqlConnection con = new NpgsqlConnection(CONNECTION_STRING))
			{
				con.Open();
				List<ImageModel> images = new List<ImageModel>();
				using (NpgsqlCommand cmd = con.CreateCommand())
				{
					cmd.CommandText = $"SELECT id, fileuri, name FROM images WHERE isnsfw=false ORDER BY timeadded DESC OFFSET {count * page} LIMIT {count}";
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

		/// <summary>
		/// Get all images in the DB, unsorted
		/// </summary>
		/// <returns>List of completed ImageModels</returns>
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

		/// <summary>
		/// Search the DB for a given query
		/// </summary>
		/// <param name="query">The query to use</param>
		/// <param name="count">Number of images to get</param>
		/// <param name="page">The page to get</param>
		/// <returns>List of completed ImageModels</returns>
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
					cmd.CommandText = $"SELECT I.id, I.fileUri, I.name, I.isnsfw, I.timeadded FROM images I {(string.IsNullOrWhiteSpace(cleanQuery) ? "" : $"WHERE {cleanQuery}")} ORDER BY I.timeadded DESC OFFSET {count * page} LIMIT {count}";
					using (var reader = cmd.ExecuteReader())
					{
						if (!reader.HasRows)
							return images;
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



		public static List<TagModel> GetTags(string imageId)
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
							return tags;
						while (reader.Read())
						{
							tags.Add(GetTag(reader.GetString(0)));
						}
					}
				}
			}
			return tags;
		}

		public static TagModel GetTag(string tagID)
		{
			TagModel tag = null;
			using (NpgsqlConnection con = new NpgsqlConnection(CONNECTION_STRING))
			{
				con.Open();
				using (NpgsqlCommand cmd = con.CreateCommand())
				{
					cmd.CommandText = $"SELECT name, type, description, parentID FROM tags WHERE id='{Uri.EscapeDataString(tagID)}';";
					using (var reader = cmd.ExecuteReader())
					{
						if (!reader.HasRows)
							return null;
						while (reader.Read())
						{
							tag = new TagModel(reader.GetString(0), reader.GetString(1), reader.GetString(2))
							{
								parentID = reader.GetString(3)
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
						Write(e.Message);
					}
				}
			}
		}

		public static bool HasTag(ImageModel image, TagModel tag) => HasTag(image.id, tag.id);

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

		public static bool TagExists(TagModel tag) => TagExists(tag.name);

		public static bool TagExists(string tag)
		{
			using (NpgsqlConnection con = new NpgsqlConnection(CONNECTION_STRING))
			{
				con.Open();
				using (NpgsqlCommand cmd = con.CreateCommand())
				{
					cmd.CommandText = $"SELECT EXISTS (SELECT id FROM tags WHERE id='{tag.ToBase60()}');";
					return (bool)cmd.ExecuteScalar();
				}
			}
		}

		public static bool ImageExists(ImageModel image) => ImageExists(image.name);

		public static bool ImageExists(string image)
		{
			using (NpgsqlConnection con = new NpgsqlConnection(CONNECTION_STRING))
			{
				con.Open();
				using (NpgsqlCommand cmd = con.CreateCommand())
				{
					cmd.CommandText = $"SELECT EXISTS (SELECT id FROM images WHERE id='{image.ToBase60()}');";
					return (bool)cmd.ExecuteScalar();
				}
			}
		}
	}
}
