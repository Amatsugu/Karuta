using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Drawing;
using System.Drawing.Imaging;
using System.Threading;
using System.Threading.Tasks;
using System.Text.RegularExpressions;
using RedditSharp;
using RedditSharp.Things;
using Imgur.API.Authentication.Impl;
using Imgur.API.Endpoints.Impl;
using com.LuminousVector.Karuta.Commands;

namespace com.LuminousVector.Karuta
{
	public class CrawlerCommand : Command
	{
		private RedditCrawler crawler;

		public CrawlerCommand() : base("crawl", "Connects to reddit and downloads images from specified subreddits")
		{
			crawler = new RedditCrawler();
			RegisterOption('c', crawler.SetPostsToGet, "Sets the number of posts to get");
			RegisterOption('v', crawler.SetVerbose, "Toggles Verbose logging mode");
			RegisterOption('a', crawler.AddSub, "Add a subredit");
			RegisterOption('m', crawler.SetSearchMode, "Set the search mode");
			RegisterOption('r', crawler.RemoveSub, "Remove a subreddit");
			RegisterOption('f', crawler.SetGetFrom, "Sets a single subreddit to be crawled from");
			RegisterOption('d', crawler.SetSaveDir, "Sets the save location for images");
			RegisterOption('t', crawler.SetUpdateRate, "Sets the interval at which the bot will cycle in minutes");

			RegisterKeyword("get", crawler.Get, "Get images from subreddit(s) without looping");
			RegisterKeyword("list", crawler.ListSubs, "Shows the list of all subreddits to be searched");
			RegisterKeyword("status", crawler.Status, "Shows the current status of the bot");
			RegisterKeyword("start", crawler.Start, "Start a crawl of all subreddits on the list at a set interval");
			RegisterKeyword("stop", crawler.Stop, "Stop all processes");
			RegisterKeyword("imgur", crawler.ImgurSetup, "Link Imgur API");
		}

		public override void Stop()
		{
			crawler.Close();
		}
	}

	class RedditCrawler
	{
		public string name = "RedditCrawler";
		public bool isRunning = false;
		public bool needsReBuild = false;
		public bool loop = true;
		public List<string> subreddits = new List<string>();
		public string[] allowedFiles = new string[] { ".png", ".jpg", ".jpeg", ".gif"};
		public string baseDir = @"K:/RedditCrawl";
		public int updateRate = 60 * 60 * 1000; //1 hour
		public int postsToGet = 100;
		public bool verbose = false;
		public SearchMode searchMode = SearchMode.New;
		public byte[] data;
		public string getFrom;
		int imgCount = 0;
		private Timer _crawlLoop;




		public enum SearchMode
		{
			New, Hot, Top
		}
		
		private Reddit _reddit;
		private WebClient _client;
		private ImgurClient _imgurClient;

		public void Setup()
		{
			try
			{
				Karuta.Write("Connecting to reddit...");
				string user = Karuta.registry.GetString("reddit_user");
				string pass = Karuta.registry.GetString("reddit_pass");
				if(user == "" || pass == "")
				{
					Karuta.Write("Please enter your reddit Credentials");
					user = Karuta.GetInput("Username");
					pass = Karuta.GetInput("Pass", true);
				}
				try
				{
					_reddit = new Reddit(user, pass);
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
				if (Karuta.registry.GetString("baseDir") != "")
					baseDir = Karuta.registry.GetString("baseDir");
				if (Karuta.registry.GetInt("updateRate") != default(int))
					updateRate = Karuta.registry.GetInt("updateRate");
				int c = Karuta.registry.GetInt("reddit_postsToGet");
				if (c == default(int))
					c = 100;
				postsToGet = c;
				string mode = Karuta.registry.GetString("reddit_searchMode");
				SetSearchMode(mode == "" ? "new" : mode);
				string list = Karuta.registry.GetString("reddit_subs");
				if (list != "")
				{
					subreddits.AddRange(list.Split('|'));
				}
				Karuta.Write("Done...");
			}
			catch (Exception e)
			{
				Karuta.Write("Failed to Connect");
				Karuta.Write(e.StackTrace);
			}
		}

		public void ImgurSetup()
		{
			string imgID = Karuta.registry.GetString("imgur_id");
			string imgSec = Karuta.registry.GetString("imgur_secret");
			if (imgID == "" || imgSec == "")
			{
				Karuta.Write("Please enter Imgur API information:");
				imgID = Karuta.GetInput("Imgur API ID");
				imgSec = Karuta.GetInput("Imgur API Sec");
			}
			Karuta.Write("Connecting to imgur...");
			try
			{
				_imgurClient = new ImgurClient(imgID, imgSec);
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
				Karuta.Write(sub + " is not on the list.");
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
				Karuta.Write("Getting " + postsToGet + " images from " + subreddits.Count + " subreddits.");
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
				Karuta.Write("Crawlings across " + postsToGet + " images from " + subreddits.Count + " subreddits.");
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
			Karuta.Write("Running: " + isRunning);
			Karuta.Write("Loop: " + loop);
			Karuta.Write("Needs Rebuild: " + needsReBuild);
			Karuta.Write("Posts to Get: " + postsToGet);
			Karuta.Write("Search Mode: " + searchMode);
			Karuta.Write("Verbose: " + verbose);
			Karuta.Write("Get From: " + ((getFrom == null) ? "all" : getFrom));
			Karuta.Write("Save Dir: " + baseDir);
			Karuta.Write("Update Rate: " + updateRate + "ms");
		}

		//Stop all processes
		public void Stop()
		{
			Karuta.logger.Log("Crawl terminated by " + Karuta.user, name);
			isRunning = false;
			_client.Dispose();
			_crawlLoop.Dispose();
		}

		//Start find and download the images
		void Crawl()
		{
			if (_reddit == null || _imgurClient == null)
				Setup();
			if (getFrom != null)
				Karuta.logger.Log("Starting crawl of " + getFrom, name, verbose);
			else
				Karuta.logger.Log("Starting Crawl of " + subreddits.Count + " subreddits", name, verbose);
			string curDir = "";
			imgCount = 0;
			string file = "";
			Listing<Post> posts = default(Listing<Post>);
			List<Subreddit> subs;
			TimeSpan t;
			DateTime minTime = new DateTime(1970, 1, 1);
			int epoch;
			bool postGet = false;
			ImageEndpoint imgEndpoint = new ImageEndpoint(_imgurClient);
			AlbumEndpoint albumEndpoint = new AlbumEndpoint(_imgurClient);
			subs = new List<Subreddit>();
			_crawlLoop?.Dispose();
			_crawlLoop = new Timer(info => 
			{
				try
				{
					imgCount = 0;
					if(needsReBuild)
					{
						subs.Clear();
						Karuta.logger.Log("Rebuilding subreddit list", name, verbose);
						if (getFrom != null)
						{
							try
							{
								subs.Add(_reddit.GetSubreddit(getFrom));
								Karuta.logger.Log("Connected to " + getFrom, name, verbose);
							}
							catch (Exception e)
							{
								Karuta.logger.LogWarning("Failed to connect to subreddit: " + getFrom + ", " + e.Message, name, verbose);
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
									Karuta.logger.Log("Connected to " + s, name, verbose);
								}
								catch (Exception e)
								{
									Karuta.logger.LogWarning("Failed to connect to subreddit: " + s + ", " + e.Message, name, verbose);
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
						curDir = baseDir + "/" + sub.Name;
						if (!Directory.Exists(curDir))
							Directory.CreateDirectory(curDir);
						bool subCollected = false;
						while (!subCollected)
						{
							if (!isRunning)
							{
								break;
							}
							try
							{
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
							}catch(Exception e)
							{
								Karuta.logger.LogWarning("Failed to connect to reddit: " + e.Message + ", retrying...", name, verbose);
							}
						}
						postGet = false;
						while(!postGet)
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
									//Calculate epoch time
									t = p.CreatedUTC - minTime;
									epoch = (int)t.TotalSeconds;

									if (allowedFiles.Contains(ext)) //Direct link to image file
									{
										file = "[" + epoch + "] " + file;
										file = curDir + ((p.NSFW) ? "/NSFW" : "") + "/" + file;
										if (File.Exists(file + ((ext == ".gif") ? ext : ".png")))
										{
											//Karuta.logger.LogWarning("Skipping \"" + p.Title + "\", file already exsits", "/r/" + sub.Name, verbose);
											continue;
										}
										//Karuta.logger.Log("Saving: " + log, "/r/" + sub.Name, _verbose);
										SaveImage(p, file, p.Url);
									}else if(p.Url.DnsSafeHost == "imgur.com") //Imgur in-direct link/album
									{
										string imgurID = Path.GetFileNameWithoutExtension(p.Url.AbsolutePath);
										if (p.Url.AbsolutePath.Contains("/a/") || p.Url.AbsolutePath.Contains("/gallery/")) //Save Imgur Album
										{
											try
											{
												imgurID = p.Url.AbsolutePath;
												imgurID = imgurID.Replace("/a/", "");
												imgurID = imgurID.Replace("/gallery/", "");
												imgurID = imgurID.Replace("/new", "");
												imgurID = imgurID.Replace("/", "");
												if (imgurID.Length < 3)
													continue;
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
													string thisFile = "[" + epoch + "] [" + i + "] " + file;
													thisFile = curDir + ((p.NSFW) ? "/NSFW" : "") + "/" + thisFile;
													//Karuta.logger.Log(thisFile, name, verbose);
													ext = Path.GetExtension(image.Link);
													if (File.Exists(thisFile + ((ext == ".gif") ? ext : ".png")))
													{
														//Karuta.logger.LogWarning("Skipping \"" + p.Title + "\", file already exsits", "/r/" + sub.Name, verbose);
														i++;
														continue;
													}
													SaveImage(p, thisFile, new Uri(image.Link));
													i++;
												}
											}catch(Exception e)
											{
												Karuta.logger.LogWarning("Unable to Download " + p.Title + ", " + e.Message, "/r/" + p.SubredditName, verbose);
												Karuta.logger.LogError(e.StackTrace, name, verbose);
											}
										}
										else
										{
											if(imgurID != "new")//Save Imgur in-drect link
											{
												file = "[" + epoch + "] " + file;
												file = curDir + ((p.NSFW) ? "/NSFW" : "") + "/" + file;
												
												try
												{
													var task = imgEndpoint.GetImageAsync(imgurID);
													Task.WaitAll(task);
													var image = task.Result;
													ext = Path.GetExtension(image.Link);
													if (File.Exists(file + ((ext == ".gif") ? ext : ".png")))
													{
														//Karuta.logger.LogWarning("Skipping \"" + p.Title + "\", file already exsits", "/r/" + sub.Name, verbose);
														continue;
													}
													//Karuta.logger.Log("Saving: " + log, "/r/" + sub.Name, _verbose);
													SaveImage(p, file, new Uri(image.Link));
												}catch(Exception e)
												{
													Karuta.logger.LogWarning("Unable to Download " + p.Title + ", " + e.Message, "/r/" + p.SubredditName, verbose);
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
								Karuta.logger.LogWarning("Failed to get posts: " + e.Message + ", retrying...", name, verbose);
								postGet = false;
							}
						}
					}
				}catch(Exception e)
				{
					Karuta.logger.LogError("Crawl failed... " + e.Message + ", shutting down", name, verbose);
					Karuta.logger.LogError(e.StackTrace, name, verbose);
					Karuta.Write("Crawl Ended");
					isRunning = false;
				}
				if (!isRunning)
					_crawlLoop.Dispose();
				
				if (!loop)
				{
					Karuta.logger.Log("Finished Dowloading " + imgCount + " images... shutting down", name, verbose);
					isRunning = false;
					loop = true;
				}
				else
				{
					Karuta.logger.Log("Dowloaded " + imgCount + " images...", name, verbose);
					Karuta.logger.Log("Sleeping for " + updateRate + "ms", name, verbose);
				}
			}, null, 0, updateRate);
		}

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
							if (Path.GetExtension(url.AbsolutePath) == ".gif")
							{
								image.Save(file + ".gif", ImageFormat.Gif);
								Karuta.logger.Log("Saved " + file + ".gif", "/r/" + p.SubredditName, verbose);
							}
							else
							{
								image.Save(file + ".png", ImageFormat.Png);
								Karuta.logger.Log("Saved " + file + ".png", "/r/" + p.SubredditName, verbose);
							}
							imgCount++;
						}
						catch (Exception e)
						{
							Karuta.logger.LogError("Failed to save \"" + p.Title + "\", " + e.Message, "/r/" + p.SubredditName, verbose);
							//Karuta.logger.LogError(e.StackTrace, name, verbose);
						}
					}
				}
			}
			catch (Exception e)
			{
				Karuta.logger.LogWarning("Unable to Download " + p.Title + ", " + e.Message, "/r/" + p.SubredditName, verbose);
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
