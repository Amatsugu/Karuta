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
using static LuminousVector.Karuta.Karuta;
using LuminousVector.Karuta.Commands;
using LuminousVector.Utils.Extensions;
using LuminousVector.RinDB.Models;

namespace LuminousVector.RinDB
{
	[KarutaCommand(Name = "crawl")]
	public sealed class CrawlerCommand : Command, IDisposable
	{
		private RedditCrawler crawler;
		private bool _autoStart = false;

		public CrawlerCommand() : base("crawl", "Connects to reddit and downloads images from specified subreddits")
		{
			bool? auto = REGISTY.GetBool("redditAutostart");
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
				REGISTY.SetValue("redditAutostart", _autoStart);
				Write("Autostart " + ((_autoStart) ? "enabled" : "disabled"));
			}, "enable/disable autostart");
			RegisterKeyword("imgur", () =>
			{
				Write("Reset Imgur");
				REGISTY.SetValue("imgur_id", "");
				REGISTY.SetValue("imgur_secret", "");
				crawler.ImgurSetup();
			}, "Link Imgur API");
			init = () =>
			{
				if (_autoStart)
					crawler.Start();
			};
		}

		public void Dispose()
		{
			crawler.Close();
		}

		public override void Stop()
		{
			Dispose();
		}
	}

	class RedditCrawler : IDisposable
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
				Write("Connecting to reddit... ", false);
				string user = REGISTY.GetString("reddit_user");
				string pass = REGISTY.GetString("reddit_pass");
				if(string.IsNullOrWhiteSpace(user) || string.IsNullOrWhiteSpace(pass))
				{
					Write("Please enter your reddit Credentials");
					user = GetInput("Username");
					pass = GetInput("Pass", true);
				}
				try
				{
					_reddit = new Reddit(); //TODO: implement OAuth
					
				}catch(Exception e)
				{
					Write("Failed!");
					Write(e.Message);
					Write("Try again");
					return;
				}
				Write("Done!");

				REGISTY.SetValue("reddit_user", user);
				REGISTY.SetValue("reddit_pass", pass);
				ImgurSetup();
				Write("Loading Prefs... ", false);
				if (!string.IsNullOrWhiteSpace(REGISTY.GetString("baseDir")))
					baseDir = REGISTY.GetString("baseDir");
				if (REGISTY.GetInt("updateRate") != null)
					updateRate = (int)REGISTY.GetInt("updateRate");
				int c;
				if (REGISTY.GetInt("reddit_postsToGet") == null)
					c = 100;
				else
					c = (int)REGISTY.GetInt("reddit_postsToGet");
				postsToGet = c;
				string mode = REGISTY.GetString("reddit_searchMode");
				SetSearchMode(string.IsNullOrWhiteSpace(mode) ? "new" : mode);
				LoadSubs();

				Write("Done!");
			}
			catch (Exception e)
			{
				Write("Failed to Start" + e.Message);
				Write(e.StackTrace);
			}
		}

		private void LoadSubs()
		{
			string oldData = REGISTY.GetString("reddit_subs");
			if (!string.IsNullOrWhiteSpace(oldData))
			{
				subreddits.AddRange(oldData.Split('|'));
			}
		}

		public void ImgurSetup()
		{
			string imgID = REGISTY.GetString("imgur_id");
			string imgSec = REGISTY.GetString("imgur_secret");
			if (string.IsNullOrWhiteSpace(imgID) || string.IsNullOrWhiteSpace(imgSec))
			{
				Write("Please enter Imgur API information:");
				imgID = GetInput("Imgur API ID");
				imgSec = GetInput("Imgur API Sec");
			}
			Write("Connecting to imgur... ", false);
			try
			{
				_imgurClient = new ImgurClient(imgID, imgSec);
				albumEndpoint = new AlbumEndpoint(_imgurClient);
			}
			catch (Exception e)
			{
				Write("Failed!");
				Write(e.Message);
				_imgurClient = null;
			}
			Write("Done!");
			REGISTY.SetValue("imgur_id", imgID);
			REGISTY.SetValue("imgur_secret", imgSec);
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
			REGISTY.SetValue("baseDir", dir);
		}

		//Set Update Rate
		public void SetUpdateRate(string rate)
		{
			if (_reddit == null || _imgurClient == null)
				Setup();
			int updateRate;
			if (int.TryParse(rate, out updateRate))
				this.updateRate = updateRate * 60 * 1000;
			REGISTY.SetValue("updateRate", this.updateRate);
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
					Write("Invalid Mode, search mode unchanged");
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
			Write("Added " + sub);
			SaveSubs();
			needsReBuild = true;
		}

		private void SaveSubs()
		{
			string subs = "";
			foreach (string s in subreddits)
			{
				if (subs == "")
					subs += s;
				else
					subs += "|" + s;
			}
			REGISTY.SetValue("reddit_subs", subs);
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
				SaveSubs();
				needsReBuild = true;
				Write(sub + " removed");
			}
			else
				Write($"{sub} is not on the list.");
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
				Write($"Getting {postsToGet} images from {subreddits.Count} subreddits.");
			}
			else
				Write("The crawller is already running...");
		}

		//List all subreddits
		public void ListSubs()
		{
			if (_reddit == null || _imgurClient == null)
				Setup();
			foreach (string s in subreddits)
			{
				Write(s);
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
				Write($"Crawlings across {postsToGet} images from {subreddits.Count} subreddits.");
			}
			else
			{
				Write("The crawller is already running...");
			}
		}

		//Shows the current status of the crawler
		public void Status()
		{
			if (_reddit == null || _imgurClient == null)
				Setup();
			Write($"Running: {isRunning}");
			Write($"Loop: {loop}");
			Write($"Needs Rebuild: {needsReBuild}");
			Write($"Posts to Get: {postsToGet}");
			Write($"Search Mode: {searchMode}");
			Write($"Verbose: {verbose}");
			Write($"Get From: {((getFrom == null) ? "all" : getFrom)}");
			Write($"Save Dir: {baseDir}");
			Write($"Update Rate: {updateRate}ms");
		}

		//Stop all processes
		public void Stop()
		{
			LOGGER.Log($"Crawl terminated by {USER}", name);
			isRunning = false;
			StopTimer("RedditCrawler");
			_client.Dispose();
			_crawlLoop = null;
		}

		//Start find and download the images
		void Crawl()
		{
			if (_reddit == null || _imgurClient == null)
				Setup();
			if (getFrom != null)
				LOGGER.Log($"Starting crawl of {getFrom}", name, verbose);
			else
				LOGGER.Log($"Starting Crawl of {subreddits.Count} subreddits", name, verbose);
			string curDir = "";
			imgCount = 0;
			string file = "";
			Listing<Post> posts = default(Listing<Post>);
			List<Subreddit> subs;
			bool postGet = false;
			
			subs = new List<Subreddit>();
			_crawlLoop = StartTimer("RedditCrawler", info =>
			{
				try
				{
					
					imgCount = 0;
					if (needsReBuild)
					{
						subs.Clear();
						LOGGER.Log("Rebuilding subreddit list", name, verbose);
						if (getFrom != null)
						{
							try
							{
								subs.Add(_reddit.GetSubreddit(getFrom));
								LOGGER.Log($"Connected to {getFrom}", name, verbose);
							}
							catch (Exception e)
							{
								LOGGER.LogWarning($"Failed to connect to subreddit: {getFrom}, {e.Message}", name, verbose);
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
									LOGGER.Log($"Connected to {s}", name, verbose);
								}
								catch (Exception e)
								{
									LOGGER.LogWarning($"Failed to connect to subreddit: {s}, {e.Message}", name, verbose);
									LOGGER.LogWarning(e.StackTrace, name, verbose);
									continue;
								}
							}
						}
						needsReBuild = false;
						LOGGER.Log("Finished Building, starting crawl", name, verbose);
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
									Write("Retry timeout");
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
								LOGGER.LogWarning($"Failed to connect to reddit: {e.Message}, retrying...", name, verbose);
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
									file = FormatFileName(p.Title);
									if (allowedFiles.Contains(ext)) //Direct link to image file
									{
										file = "[" + p.CreatedUTC.ToEpoch() + "] " + file;
										file = curDir + ((p.NSFW) ? "/NSFW" : "") + "/" + file;
										if (File.Exists($"{file}{ext}"))
										{
											//logger.LogWarning("Skipping \"" + p.Title + "\", file already exsits", "/r/" + sub.Name, verbose);
											continue;
										}
										//logger.Log("Saving: " + log, "/r/" + sub.Name, _verbose);
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
													string link = $"http://i.imgur.com/{imgurID}.png";
													ext = Path.GetExtension(link);
													if (File.Exists($"{file}{ext}"))
													{
														//logger.LogWarning("Skipping \"" + p.Title + "\", file already exsits", "/r/" + sub.Name, verbose);
														continue;
													}
													//logger.Log("Saving: " + log, "/r/" + sub.Name, _verbose);
													SaveImage(p, file, new Uri(link));
													
												}
												catch (Exception e)
												{
													LOGGER.LogWarning($"Unable to Download {p.Title}, {e.Message}", $"/r/{p.SubredditName}", verbose);
													LOGGER.LogError(e.StackTrace, name, verbose);
												}
											}
										}
									}
									file = "";
								}
							}
							catch (Exception e)
							{
								LOGGER.LogWarning($"Failed to get posts: {e.Message}, retrying...", name, verbose);
								postGet = false;
							}
						}
					}
				}
				catch (Exception e)
				{
					LOGGER.LogError($"Crawl failed... {e.Message}, shutting down", name, verbose);
					LOGGER.LogError(e.StackTrace, name, verbose);
					Write("Crawl Ended");
					isRunning = false;
				}
				if (!isRunning && _crawlLoop != null)
					StopTimer("RedditCrawler");

				if (!loop)
				{
					LOGGER.Log($"Finished Dowloading {imgCount} images... shutting down", name, verbose);
					isRunning = false;
					loop = true;
				}
				else
				{
					LOGGER.Log($"Dowloaded {imgCount} images...", name, verbose);
					LOGGER.Log($"Sleeping for {updateRate}ms", name, verbose);
				}
			}, 0, updateRate);
		}

		private string FormatFileName(string name)
		{
			return name.Replace("/r/", "")
				.Replace("?", "")
				.Replace("*", "")
				.Replace("!", "")
				.Replace("/", "")
				.Replace(":", "")
				.Replace("\"", "'")
				.Replace("<", "")
				.Replace(">", "")
				.Replace("|", "")
				.Replace("\\", "");
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
				case "megumin":
					_curTags = new TagModel[] { new TagModel("Megumin"), new TagModel("KonoSuba") };
					break;
				case "oregairusnafu":
					_curTags = new TagModel[] { new TagModel("Ore Gairu SNAFU") };
					break;
				case "SukumizuIRL":
					_curTags = new TagModel[] { new TagModel("Sukumizu"), new TagModel("IRL") };
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
				//logger.Log("Saving Album: " + log, "/r/" + sub.Name, _verbose);
				//logger.Log("Album ID: " + imgurID, "/r/" + sub.Name, _verbose);
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
					//logger.Log(thisFile, name, verbose);
					string ext = Path.GetExtension(image.Link);
					if (File.Exists(thisFile + ((ext == ".gif") ? ext : ".png")))
					{
						//logger.LogWarning("Skipping \"" + p.Title + "\", file already exsits", "/r/" + sub.Name, verbose);
						i++;
						continue;
					}
					SaveImage(p, thisFile, new Uri(image.Link));
					i++;
				}
			}
			catch (Exception e)
			{
				LOGGER.LogWarning($"Unable to Download {p.Title}, {e.Message}", $"/r/{p.SubredditName}", verbose);
				LOGGER.LogError(e.StackTrace, name, verbose);
			}
		}

		//Download an image via url
		void SaveImage(Post p, string file, Uri url)
		{
			try
			{
				data = _client.DownloadData(url);
				string ext = Path.GetExtension(url.AbsolutePath);
				using (FileStream image = new FileStream($"{file}{ext}", FileMode.Create, FileAccess.Write))
				{
					try
					{
						if (!Directory.GetParent(file).Exists)
						{
							Directory.GetParent(file).Create();
						}
						image.Write(data, 0, data.Length);
						LOGGER.Log($"Saved {file}{ext}", $"/r/{p.SubredditName}", verbose);
						image.Flush();
						RinDB.AddImage(new ImageModel()
						{
							name = Uri.EscapeDataString(Path.GetFileNameWithoutExtension(file).Remove(0, p.CreatedUTC.ToEpoch().ToString().Length + 2)),
							fileUri = Uri.EscapeDataString($"{file.Replace($"{baseDir}/", "")}{ext}"),
							timeadded = p.CreatedUTC.ToEpoch(),
							isnsfw = p.NSFW,
							tags = _curTags?.ToList()
						});
						imgCount++;
					}catch(Exception e)
					{
						LOGGER.LogError($"Failed to save \"{p.Title}\", {e.Message}", $"/r/{p.SubredditName}", verbose);
						LOGGER.LogError(e.StackTrace, name, verbose);
					}
				}
			}
			catch (Exception e)
			{
				LOGGER.LogWarning($"Unable to Download {p.Title}, {e.Message}", $"/r/{p.SubredditName}", verbose);
				LOGGER.LogError(e.StackTrace, name, verbose);
			}
		}

		//Stop the downloads if karuta is closed
		public void Close()
		{
			LOGGER.Log("Crawl terminated by Karuta", name);
			if (!isRunning)
				return;
			isRunning = false;
			_client.Dispose();
			_crawlLoop.Dispose();
		}

		public void Dispose()
		{
			Close();
		}
	}
}
