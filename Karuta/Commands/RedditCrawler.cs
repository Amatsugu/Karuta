using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Drawing;
using System.Drawing.Imaging;
using System.Threading;
using System.Threading.Tasks;
using RedditSharp;
using RedditSharp.Things;
using Imgur.API.Authentication.Impl;
using Imgur.API.Endpoints.Impl;
using LuminousVector.Karuta.Commands;
using LuminousVector.Karuta.RinDB;
using LuminousVector.Karuta.RinDB.Models;

namespace LuminousVector.Karuta
{
	[KarutaCommand(Name = "crawl")]
	public class CrawlerCommand : Command
	{
		private RedditCrawler crawler;
		private bool _autoStart = false;

		public CrawlerCommand() : base("crawl", "Connects to reddit and downloads images from specified subreddits")
		{
			bool? auto = Karuta.registry.GetBool("redditAutostart");
			_autoStart = (auto == null) ? false : (auto == true) ? true : false;
			crawler = new RedditCrawler();
			RegisterOption('c', crawler.SetPostsToGet, "Sets the number of posts to get");
			RegisterOption('v', crawler.SetVerbose, "Toggles Verbose logging mode");
			RegisterOption('a', crawler.AddSub, "Add a subredit");
			RegisterOption('m', crawler.SetSearchMode, "Set the search mode");
			RegisterOption('r', crawler.RemoveSub, "Remove a subreddit");
			RegisterOption('f', crawler.SetGetFrom, "Sets a single subreddit to be crawled from");
			RegisterOption('d', crawler.SetSaveDir, "Sets the save location for images");
			RegisterOption('t', crawler.SetUpdateRate, "Sets the interval at which the bot will cycle in minutes");
			RegisterOption('i', crawler.AlbumDownload, "Downloads and album from imgur");

			RegisterKeyword("get", crawler.Get, "Get images from subreddit(s) without looping");
			RegisterKeyword("list", crawler.ListSubs, "Shows the list of all subreddits to be searched");
			RegisterKeyword("status", crawler.Status, "Shows the current status of the bot");
			RegisterKeyword("start", crawler.Start, "Start a crawl of all subreddits on the list at a set interval");
			RegisterKeyword("stop", crawler.Stop, "Stop all processes");
			RegisterKeyword("autostart", () =>
			{
				_autoStart = !_autoStart;
				Karuta.registry.SetValue("redditAutostart", _autoStart);
				Karuta.Write("Autostart " + ((_autoStart) ? "enabled" : "disabled"));
			}, "enable/disable autostart");
			RegisterKeyword("imgur", () =>
			{
				Karuta.Write("Reset Imgur");
				Karuta.registry.SetValue("imgur_id", "");
				Karuta.registry.SetValue("imgur_secret", "");
				crawler.ImgurSetup();
			}, "Link Imgur API");

			init = () =>
			{
				if (_autoStart)
					crawler.Start();
			};
		}

		public override void Stop()
		{
			crawler.Close();
		}
	}

	class RedditCrawler
	{
		private string name = "RedditCrawler";
		private bool isRunning = false;
		private bool needsReBuild = false;
		private bool loop = true;
		private List<string> subreddits = new List<string>();
		private string[] allowedFiles = new string[] { ".png", ".jpg", ".jpeg", ".gif"};
		private string baseDir = @"K:/RedditCrawl";
		private int updateRate = 60 * 60 * 1000; //1 hour
		private int postsToGet = 100;
		private bool verbose = false;
		private byte[] data;
		private string getFrom;
		private int imgCount = 0;
		private Timer _crawlLoop;
		private AlbumEndpoint albumEndpoint;
		private SearchMode searchMode = SearchMode.New;

		private enum SearchMode
		{
			New, Hot, Top
		}
		
		private Reddit _reddit;
		private WebClient _client;
		private ImgurClient _imgurClient;
		private TagModel[] _curTags;

		public void Setup()
		{
			try
			{
				Karuta.Write("Connecting to reddit...");
				string user = Karuta.registry.GetString("reddit_user");
				string pass = Karuta.registry.GetString("reddit_pass");
				if(string.IsNullOrWhiteSpace(user) || string.IsNullOrWhiteSpace(pass))
				{
					Karuta.Write("Please enter your reddit Credentials");
					user = Karuta.GetInput("Username");
					pass = Karuta.GetInput("Pass", true);
				}
				try
				{
					_reddit = new Reddit(user, pass);
					Karuta.Write(_reddit.RateLimit);
				}catch(Exception e)
				{
					Karuta.Write(e.Message);
					Karuta.Write("Try again");
					return;
				}

				Karuta.registry.SetValue("reddit_user", user);
				Karuta.registry.SetValue("reddit_pass", pass);
				ImgurSetup();
				Karuta.Write("Loading Prefs...");
				if (!string.IsNullOrWhiteSpace(Karuta.registry.GetString("baseDir")))
					baseDir = Karuta.registry.GetString("baseDir");
				if (Karuta.registry.GetInt("updateRate") != null)
					updateRate = (int)Karuta.registry.GetInt("updateRate");
				int c;
				if (Karuta.registry.GetInt("reddit_postsToGet") == null)
					c = 100;
				else
					c = (int)Karuta.registry.GetInt("reddit_postsToGet");
				postsToGet = c;
				string mode = Karuta.registry.GetString("reddit_searchMode");
				SetSearchMode(string.IsNullOrWhiteSpace(mode) ? "new" : mode);
				string list = Karuta.registry.GetString("reddit_subs");
				if (!string.IsNullOrWhiteSpace(list))
				{
					subreddits.AddRange(list.Split('|'));
				}
				Karuta.Write("Done...");
			}
			catch (Exception e)
			{
				Karuta.Write("Failed to Start" + e.Message);
				Karuta.Write(e.StackTrace);
			}
		}

		public void ImgurSetup()
		{
			string imgID = Karuta.registry.GetString("imgur_id");
			string imgSec = Karuta.registry.GetString("imgur_secret");
			if (string.IsNullOrWhiteSpace(imgID) || string.IsNullOrWhiteSpace(imgSec))
			{
				Karuta.Write("Please enter Imgur API information:");
				imgID = Karuta.GetInput("Imgur API ID");
				imgSec = Karuta.GetInput("Imgur API Sec");
			}
			Karuta.Write("Connecting to imgur...");
			try
			{
				_imgurClient = new ImgurClient(imgID, imgSec);
				Karuta.Write(_imgurClient.RateLimit);
				albumEndpoint = new AlbumEndpoint(_imgurClient);
			}
			catch (Exception e)
			{
				Karuta.Write("Failed to connect");
				Karuta.Write(e.Message);
				_imgurClient = null;
			}

			Karuta.registry.SetValue("imgur_id", imgID);
			Karuta.registry.SetValue("imgur_secret", imgSec);
		}

		//Download an album directly
		public void AlbumDownload(string url)
		{
			if (_reddit == null || _imgurClient == null)
				Setup();
			Post p = new Post()
			{
				SubredditName = "Imgur",
				NSFW = false,
				CreatedUTC = DateTime.UtcNow
			};

			string curDir = baseDir + "/ImgurDownload";
			isRunning = true;
			_client = new WebClient();
			DownloadImgurAlbum(new Uri(url), "Imgur", p, curDir);
			isRunning = false;
		}

		//Set verbose
		public void SetVerbose()
		{
			if (_reddit == null || _imgurClient == null)
				Setup();
			verbose = !verbose;
		}

		//Set Save Dir
		public void SetSaveDir(string dir)
		{
			if (_reddit == null || _imgurClient == null)
				Setup();
			baseDir = dir;
			Karuta.registry.SetValue("baseDir", dir);
		}

		//Set Update Rate
		public void SetUpdateRate(string rate)
		{
			if (_reddit == null || _imgurClient == null)
				Setup();
			int updateRate;
			if (int.TryParse(rate, out updateRate))
				this.updateRate = updateRate * 60 * 1000;
			Karuta.registry.SetValue("updateRate", this.updateRate);
		}

		//Set GetFrom sub
		public void SetGetFrom(string sub)
		{
			if (_reddit == null || _imgurClient == null)
				Setup();
			if (!sub.Contains("/r/"))
				sub = "/r/" + sub;
			getFrom = sub;
		}

		//Set max posts to get
		public void SetPostsToGet(string count)
		{
			if (_reddit == null || _imgurClient == null)
				Setup();
			int c = -1;
			if(int.TryParse(count, out c))
			{
				postsToGet = c;
			}
		}

		//Set the search mode
		public void SetSearchMode(string mode)
		{
			if (_reddit == null || _imgurClient == null)
				Setup();
			switch (mode.ToLower())
			{
				case "top":
					searchMode = SearchMode.Top;
					break;
				case "new":
					searchMode = SearchMode.New;
					break;
				case "hot":
					searchMode = SearchMode.Hot;
					break;
				default:
					Karuta.Write("Invalid Mode, search mode unchanged");
					return;
			}
		}

		//Add and save the list of subreddits
		public void AddSub(string sub)
		{
			if (_reddit == null || _imgurClient == null)
				Setup();
			if (!sub.Contains("/r/"))
				sub = "/r/" + sub;
			subreddits.Add(sub);
			Karuta.Write("Added " + sub);
			string subs = "";
			foreach (string s in subreddits)
			{
				if (subs == "")
					subs += s;
				else
					subs += "|" + s;
			}
			Karuta.registry.SetValue("reddit_subs", subs);
			needsReBuild = true;
		}

		//Remove a subreddit
		public void RemoveSub(string sub)
		{
			if (_reddit == null || _imgurClient == null)
				Setup();
			if (!sub.Contains("/r/"))
				sub = "/r/" + sub;
			if (subreddits.Contains(sub))
			{
				subreddits.Remove(sub);
				string subs = "";
				foreach (string s in subreddits)
				{
					if (subs == "")
						subs += s;
					else
						subs += "|" + s;
				}
				Karuta.registry.SetValue("reddit_subs", subs);
				needsReBuild = true;
				Karuta.Write(sub + " removed");
			}
			else
				Karuta.Write($"{sub} is not on the list.");
		}

		//Get posts from without looping
		public void Get()
		{
			if (_reddit == null || _imgurClient == null)
				Setup();
			if (!isRunning)
			{
				needsReBuild = true;
				isRunning = true;
				loop = false;
				_client = new WebClient();
				Crawl();
				Karuta.Write($"Getting {postsToGet} images from {subreddits.Count} subreddits.");
			}
			else
				Karuta.Write("The crawller is already running...");
		}

		//List all subreddits
		public void ListSubs()
		{
			if (_reddit == null || _imgurClient == null)
				Setup();
			foreach (string s in subreddits)
			{
				Karuta.Write(s);
			}
		}

		//Start the crawl
		public void Start()
		{
			if (_reddit == null || _imgurClient == null)
				Setup();
			if (!isRunning)
			{
				needsReBuild = true;
				isRunning = true;
				loop = true;
				_client = new WebClient();
				Crawl();
				Karuta.Write($"Crawlings across {postsToGet} images from {subreddits.Count} subreddits.");
			}
			else
			{
				Karuta.Write("The crawller is already running...");
			}
		}

		//Shows the current status of the crawler
		public void Status()
		{
			if (_reddit == null || _imgurClient == null)
				Setup();
			Karuta.Write($"Running: {isRunning}");
			Karuta.Write($"Loop: {loop}");
			Karuta.Write($"Needs Rebuild: {needsReBuild}");
			Karuta.Write($"Posts to Get: {postsToGet}");
			Karuta.Write($"Search Mode: {searchMode}");
			Karuta.Write($"Verbose: {verbose}");
			Karuta.Write($"Get From: {((getFrom == null) ? "all" : getFrom)}");
			Karuta.Write($"Save Dir: {baseDir}");
			Karuta.Write($"Update Rate: {updateRate}ms");
		}

		//Stop all processes
		public void Stop()
		{
			Karuta.logger.Log($"Crawl terminated by {Karuta.user}", name);
			isRunning = false;
			Karuta.StopTimer("RedditCrawler");
			_client.Dispose();
			_crawlLoop = null;
		}

		//Start find and download the images
		void Crawl()
		{
			if (_reddit == null || _imgurClient == null)
				Setup();
			if (getFrom != null)
				Karuta.logger.Log($"Starting crawl of {getFrom}", name, verbose);
			else
				Karuta.logger.Log($"Starting Crawl of {subreddits.Count} subreddits", name, verbose);
			string curDir = "";
			imgCount = 0;
			string file = "";
			Listing<Post> posts = default(Listing<Post>);
			List<Subreddit> subs;
			bool postGet = false;
			
			subs = new List<Subreddit>();
			_crawlLoop = Karuta.StartTimer("RedditCrawler", info =>
			{
				try
				{
					
					imgCount = 0;
					if (needsReBuild)
					{
						subs.Clear();
						Karuta.logger.Log("Rebuilding subreddit list", name, verbose);
						if (getFrom != null)
						{
							try
							{
								subs.Add(_reddit.GetSubreddit(getFrom));
								Karuta.logger.Log($"Connected to {getFrom}", name, verbose);
							}
							catch (Exception e)
							{
								Karuta.logger.LogWarning($"Failed to connect to subreddit: {getFrom}, {e.Message}", name, verbose);
								_crawlLoop.Dispose();
							}
						}
						else
						{
							if (subreddits.Count == 0)
							{
								isRunning = false;
								return;
							}
							subs.Clear();
							foreach (string s in subreddits)
							{
								if (!isRunning)
									break;
								try
								{
									subs.Add(_reddit.GetSubreddit(s));
									Karuta.logger.Log($"Connected to {s}", name, verbose);
								}
								catch (Exception e)
								{
									Karuta.logger.LogWarning($"Failed to connect to subreddit: {s}, {e.Message}", name, verbose);
									Karuta.logger.LogWarning(e.StackTrace, name, verbose);
									continue;
								}
							}
						}
						needsReBuild = false;
						Karuta.logger.Log("Finished Building, starting crawl", name, verbose);
					}
					if (!isRunning)
						_crawlLoop.Dispose();
					foreach (Subreddit sub in subs)
					{
						if (!isRunning)
							break;
						SelectTags(sub.Name);
						//Change the current directory and make sure it exists 
						curDir = baseDir + "/" + sub.Name;
						if (!Directory.Exists(curDir))
							Directory.CreateDirectory(curDir);
						bool subCollected = false;
						int retryCount = 0;
						while (!subCollected)
						{
							if (!isRunning || retryCount >= 10)
							{
								if (retryCount >= 10)
									Karuta.Write("Retry timeout");
								break;
							}
							try
							{
								//Get posts 
								switch (searchMode)
								{
									case SearchMode.Hot:
										posts = sub.Hot;
										break;
									case SearchMode.New:
										posts = sub.New;
										break;
									case SearchMode.Top:
										posts = sub.GetTop(FromTime.All);
										break;
									default:
										posts = sub.New;
										break;
								}
								subCollected = true;
							}
							catch (Exception e)
							{
								Karuta.logger.LogWarning($"Failed to connect to reddit: {e.Message}, retrying...", name, verbose);
								retryCount++;
							}
						}
						postGet = false;
						while (!postGet)
						{
							if (!isRunning)
								break;
							try
							{
								foreach (Post p in posts.Take(postsToGet))
								{
									postGet = true;
									if (!isRunning)
										break;
									string log = (p.Title + " " + p.Url);
									string ext = Path.GetExtension(p.Url.AbsolutePath);
									if (p.NSFW && !Directory.Exists(curDir + "/NSFW"))
										Directory.CreateDirectory(curDir + "/NSFW");

									//Create file name
									file = p.Title;
									file = file.Replace("/r/", "");
									file = file.Replace("?", "");
									file = file.Replace("*", "");
									file = file.Replace("!", "");
									file = file.Replace("/", "");
									file = file.Replace(":", "");
									file = file.Replace("\"", "'");
									file = file.Replace("<", "");
									file = file.Replace(">", "");
									file = file.Replace("|", "");
									file = file.Replace("\\", "");

									if (allowedFiles.Contains(ext)) //Direct link to image file
									{
										file = "[" + p.CreatedUTC.ToEpoch() + "] " + file;
										file = curDir + ((p.NSFW) ? "/NSFW" : "") + "/" + file;
										if (File.Exists(file + ((ext == ".gif") ? ext : ".png")))
										{
											//Karuta.logger.LogWarning("Skipping \"" + p.Title + "\", file already exsits", "/r/" + sub.Name, verbose);
											continue;
										}
										//Karuta.logger.Log("Saving: " + log, "/r/" + sub.Name, _verbose);
										if (sub.Name.ToLower() != "hentaibeast")
										{
											RinDB.RinDB.AddImage(new ImageModel()
											{
												name = Uri.EscapeDataString(Path.GetFileNameWithoutExtension(file).Remove(0, p.CreatedUTC.ToEpoch().ToString().Length + 2)),
												fileUri = Uri.EscapeDataString($"{file.Replace($"{baseDir}/", "")}{((ext == ".gif") ? ext : ".png")}"),
												timeadded = p.CreatedUTC.ToEpoch(),
												isnsfw = p.NSFW,
												tags = _curTags
											});
										}
										SaveImage(p, file, p.Url);
									}
									else if (p.Url.DnsSafeHost == "imgur.com") //Imgur in-direct link/album
									{
										string imgurID = Path.GetFileNameWithoutExtension(p.Url.AbsolutePath);
										if (p.Url.AbsolutePath.Contains("/a/") || p.Url.AbsolutePath.Contains("/gallery/")) //Save Imgur Album
										{
											DownloadImgurAlbum(p.Url, file, p, curDir);
										}
										else
										{
											if (imgurID != "new")//Save Imgur in-drect link
											{
												file = "[" + p.CreatedUTC.ToEpoch() + "] " + file;
												file = curDir + ((p.NSFW) ? "/NSFW" : "") + "/" + file;

												try
												{
													//var task = imgEndpoint.GetImageAsync(imgurID);
													//Task.WaitAll(task);
													//var image = task.Result;
													string link = $"http://i.imgur.com/{imgurID}.png";
													ext = Path.GetExtension(link);
													if (File.Exists(file + ((ext == ".gif") ? ext : ".png")))
													{
														//Karuta.logger.LogWarning("Skipping \"" + p.Title + "\", file already exsits", "/r/" + sub.Name, verbose);
														continue;
													}
													//Karuta.logger.Log("Saving: " + log, "/r/" + sub.Name, _verbose);
													if (sub.Name.ToLower() != "hentaibeast")
													{
														RinDB.RinDB.AddImage(new ImageModel()
														{
															name = Uri.EscapeDataString(Path.GetFileNameWithoutExtension(file).Remove(0, p.CreatedUTC.ToEpoch().ToString().Length + 2)),
															fileUri = Uri.EscapeDataString($"{file.Replace($"{baseDir}/", "")}{((ext == ".gif") ? ext : ".png")}"),
															timeadded = p.CreatedUTC.ToEpoch(),
															isnsfw = p.NSFW,
															tags = _curTags
														});
													}
													SaveImage(p, file, new Uri(link));
												}
												catch (Exception e)
												{
													Karuta.logger.LogWarning($"Unable to Download {p.Title}, {e.Message}", $"/r/{p.SubredditName}", verbose);
													Karuta.logger.LogError(e.StackTrace, name, verbose);
												}
											}
										}
									}
									file = "";
								}
							}
							catch (Exception e)
							{
								Karuta.logger.LogWarning($"Failed to get posts: {e.Message}, retrying...", name, verbose);
								postGet = false;
							}
						}
					}
				}
				catch (Exception e)
				{
					Karuta.logger.LogError($"Crawl failed... {e.Message}, shutting down", name, verbose);
					Karuta.logger.LogError(e.StackTrace, name, verbose);
					Karuta.Write("Crawl Ended");
					isRunning = false;
				}
				if (!isRunning && _crawlLoop != null)
					Karuta.StopTimer("RedditCrawler");

				if (!loop)
				{
					Karuta.logger.Log($"Finished Dowloading {imgCount} images... shutting down", name, verbose);
					isRunning = false;
					loop = true;
				}
				else
				{
					Karuta.logger.Log($"Dowloaded {imgCount} images...", name, verbose);
					Karuta.logger.Log($"Sleeping for {updateRate}ms", name, verbose);
				}
			}, 0, updateRate);
		}

		private void SelectTags(string sub)
		{
			switch(sub.ToLower())
			{
				case "onetruetohsaka":
					_curTags = new TagModel[] { new TagModel("Tohsaka Rin"), new TagModel("Fate") };
					break;
				case "hatsune":
					_curTags = new TagModel[] { new TagModel("Hatsune Miku") };
					break;
				case "zettairyouiki":
					_curTags = new TagModel[] { new TagModel("Zettai Ryouiki") };
					break;
				case "zettairyouikiirl":
					_curTags = new TagModel[] { new TagModel("Zettai Ryouiki"), new TagModel("IRL") };
					break;
				case "spiceandwolf":
					_curTags = new TagModel[] { new TagModel("Spice and Wolf") };
					break;
				case "twintails":
					_curTags = new TagModel[] { new TagModel("Twin Tails") };
					break;
				case "tsundere":
					_curTags = new TagModel[] { new TagModel("Tsundere") };
					break;
				case "onetruerem":
					_curTags = new TagModel[] { new TagModel("Rem") , new TagModel("Re:Zero")};
					break;
				case "onetrueram":
					_curTags = new TagModel[] { new TagModel("Ram"), new TagModel("Re:Zero") };
					break;
				case "onetruebiribiri":
					_curTags = new TagModel[] { new TagModel("Misaka Mikoto"), new TagModel("A Certain Scientific Railgun"), new TagModel("A Certain Magical Index") };
					break;
				case "relife":
					_curTags = new TagModel[] { new TagModel("ReLife") };
					break;
				case "megane":
					_curTags = new TagModel[] { new TagModel("Megane") };
					break;
				case "kemonomimi":
					_curTags = new TagModel[] { new TagModel("Kemonomimi") };
					break;
				case "tentai":
					_curTags = new TagModel[] { new TagModel("Tentacles") };
					break;
				case "consentacles":
					_curTags = new TagModel[] { new TagModel("Consentacles") };
					break;
				default:
					_curTags = null;
					break;
			}
		}

		void DownloadImgurAlbum(Uri url, string fileName, Post p, string curDir)
		{
			try
			{
				string imgurID = url.AbsolutePath;
				imgurID = imgurID.Split('/')[2];
				if (imgurID.Length < 3)
					return;
				//Karuta.logger.Log("Saving Album: " + log, "/r/" + sub.Name, _verbose);
				//Karuta.logger.Log("Album ID: " + imgurID, "/r/" + sub.Name, _verbose);
				var task = albumEndpoint.GetAlbumImagesAsync(imgurID); 
				Task.WaitAll(task);
				var album = task.Result;
				int i = 1;
				foreach (var image in album)
				{
					if (!isRunning)
						break;
					string thisFile = "[" + p.CreatedUTC.ToEpoch() + "] [" + i + "] " + fileName;
					thisFile = curDir + ((p.NSFW) ? "/NSFW" : "") + "/" + thisFile;
					//Karuta.logger.Log(thisFile, name, verbose);
					string ext = Path.GetExtension(image.Link);
					if (File.Exists(thisFile + ((ext == ".gif") ? ext : ".png")))
					{
						//Karuta.logger.LogWarning("Skipping \"" + p.Title + "\", file already exsits", "/r/" + sub.Name, verbose);
						i++;
						continue;
					}
					if (p.SubredditName.ToLower() != "hentaibeast")
					{
						RinDB.RinDB.AddImage(new ImageModel()
						{
							name = Uri.EscapeDataString($"[{i}] {fileName}"),
							fileUri = Uri.EscapeDataString($"{thisFile.Replace($"{baseDir}/", "")}{((ext == ".gif") ? ext : ".png")}"),
							timeadded = p.CreatedUTC.ToEpoch(),
							isnsfw = p.NSFW,
							tags = _curTags
						});
					}
					SaveImage(p, thisFile, new Uri(image.Link));
					i++;
				}
			}
			catch (Exception e)
			{
				Karuta.logger.LogWarning($"Unable to Download {p.Title}, {e.Message}", $"/r/{p.SubredditName}", verbose);
				Karuta.logger.LogError(e.StackTrace, name, verbose);
			}
		}

		//Download an image via url
		void SaveImage(Post p, string file, Uri url)
		{
			try
			{
				data = _client.DownloadData(url);
				using (MemoryStream stream = new MemoryStream(data))
				{
					using (Image image = Image.FromStream(stream))
					{
						try
						{
							if(!Directory.GetParent(file).Exists)
							{
								Directory.GetParent(file).Create();
							}
							if (Path.GetExtension(url.AbsolutePath) == ".gif")
							{
								image.Save(file + ".gif", ImageFormat.Gif);
							}
							else
							{
								image.Save(file + ".png", ImageFormat.Png);
							}
							Karuta.logger.Log($"Saved {file}.{((Path.GetExtension(url.AbsolutePath) != ".gif") ? ".png" : ".gif")}", $"/r/{p.SubredditName}", verbose);
							imgCount++;
						}
						catch (Exception e)
						{
							Karuta.logger.LogError($"Failed to save \"{p.Title}\", {e.Message}", $"/r/{p.SubredditName}", verbose);
							Karuta.logger.LogError(e.StackTrace, name, verbose);
						}
					}
				}
			}
			catch (Exception e)
			{
				Karuta.logger.LogWarning($"Unable to Download {p.Title}, {e.Message}", $"/r/{p.SubredditName}", verbose);
				Karuta.logger.LogError(e.StackTrace, name, verbose);
			}
		}

		//Stop the downloads if karuta is closed
		public void Close()
		{
			Karuta.logger.Log("Crawl terminated by Karuta", name);
			if (!isRunning)
				return;
			isRunning = false;
			_client.Dispose();
			_crawlLoop.Dispose();
		}
	}
}
