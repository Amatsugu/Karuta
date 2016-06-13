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

namespace com.LuminousVector.Karuta
{
	class RedditCrawler : Command
	{
		public RedditCrawler() : base("crawl", "periodicly checks subreddits for new content and downloads them", "crawl <start/stop> {-a [subreddit]}") { }

		private bool isRunning = false;
		private bool needsReBuild = false;
		private bool loop = true;
		private List<string> subreddits = new List<string>();
		private Reddit reddit;
		private Thread thread;
		private string[] allowedFiles = new string[] { ".png", ".jpg", ".jpeg", ".gif"};
		private string baseDir = @"C:/Karuta/RedditCrawl";
		private int updateRate = 100 * 1000;
		private int postsToGet = 100;
		private bool verbose = false;
		private SearchMode searchMode = SearchMode.New;
		private byte[] data;
		int imgCount = 0;


		enum SearchMode
		{
			New, Hot, Top
		}
		
		private WebClient client;
		private ImgurClient imgurClient;

		public override void Run(string[] args)
		{
			//Initialize Reddit connection
			if(reddit == null)
			{
				try
				{
					Karuta.Write("Connecting to reddit...");
					reddit = new Reddit();
					Karuta.Write("Connecting to imgur...");
					imgurClient = new ImgurClient(Karuta.registry.GetString("imgur_id"), Karuta.registry.GetString("imgur_secret"));
					Karuta.Write("Loading Prefs...");
					string list = Karuta.registry.GetString("reddit_subs");
					int c = Karuta.registry.GetInt("reddit_postsToGet");
					if (c == default(int))
						c = 100;
					postsToGet = c;
					string mode = Karuta.registry.GetString("reddit_searchMode");
					SetSearchMode(mode == "" ? "new" : mode);
					if(list != "")
					{
						string[] subs = list.Split('|');
						subreddits.AddRange(subs);
					}
					Karuta.Write("Done...");
				}
				catch (Exception e)
				{
					Karuta.Write("Failed to Connect");
					Karuta.Write(e.StackTrace);
				}
			}

			//Add a subreddit
			if (GetIndexOfOption(args, 'a') != -1)
			{
				AddSub(args);
			}

			//Remove subbreddit
			if (GetIndexOfOption(args, 'r') != -1)
			{
				string sub = GetValueOfOption(args, 'r');
				if (sub != null)
				{
					if (!sub.Contains("/r/"))
						sub = "/r/" + sub;
					if (subreddits.Contains(sub))
					{
						subreddits.Remove(sub);
					}
					else
						Karuta.Write(sub + " is not on the list.");
				}
				else
					Karuta.Write("A subreddit must be provided");
			}

			//Toggle verbose mode
			if (GetIndexOfOption(args, 'v') != -1)
			{
				verbose = !verbose;
				Karuta.Write("Verbose: " + verbose);
			}

			//Set the number of posts to get
			if (GetIndexOfOption(args, 'c') != -1)
			{
				string count = GetValueOfOption(args, 'c');
				if (count != null)
				{
					int.TryParse(count, out postsToGet);
				}
				Karuta.Write("Set posts to get to " + postsToGet);
				Karuta.registry.SetValue("reddit_postsToGet", postsToGet);
			}

			//Set the search mode
			if(GetIndexOfOption(args, 'm') != -1)
			{
				string mode = GetValueOfOption(args, 'm');
				SetSearchMode(mode);
				Karuta.Write("Search Mode set to:" + searchMode);
				Karuta.registry.SetValue("reddit_searchMode", searchMode.ToString().ToLower());
			}


			if (args.Length == 1)
				return;

			//list all subreddits to be searched
			if(args[1] == "list")
			{
				foreach(string s in subreddits)
				{
					Karuta.Write(s);
				}
			}

			//Download all images without checking again
			if (args[1] == "get")
			{
				if (!isRunning)
				{
					needsReBuild = true;
					isRunning = true;
					loop = false;
					client = new WebClient();
					if (thread == null)
						thread = Karuta.CreateThread("RedditCrawl", new ThreadStart(Crawl));
					Karuta.Write("Getting " + postsToGet + " images from " + subreddits.Count + " subreddits.");
				}
				else
					Karuta.Write("The crawller is already running...");
			}

			//Start the crawl
			if (args[1] == "start")
			{
				if (!isRunning)
				{
					needsReBuild = true;
					isRunning = true;
					loop = true;
					client = new WebClient();
					if (thread == null)
						thread = Karuta.CreateThread("RedditCrawl", new ThreadStart(Crawl));
					Karuta.Write("Crawlings across " + postsToGet + " images from " + subreddits.Count + " subreddits.");
				}else
				{
					Karuta.Write("The crawller is already running...");
				}
			}

			//Stop all processes
			if (args[1] == "stop")
			{
				Karuta.logger.Log("Crawl terminated by " + Karuta.user, name);
				isRunning = false;
				thread.Join();
				Karuta.RemoveThread(thread);
				thread = null;
				client.Dispose();
			}

			//Shows the current status of the crawler
			if(args[1] == "status")
			{
				Karuta.Write("Running: " + isRunning);
				Karuta.Write("Loop: " + loop);
				Karuta.Write("Needs Rebuild: " + needsReBuild);
				Karuta.Write("Posts to Get: " + postsToGet);
				Karuta.Write("Search Mode: " + searchMode);
				Karuta.Write("Verbose: " + verbose);
			}
		}

		//Set the search mode
		void SetSearchMode(string mode)
		{
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
		void AddSub(string[] args)
		{
			string sub = GetValueOfOption(args, 'a');
			if (sub != null)
			{
				if (!sub.Contains("/r/"))
					sub = "/r/" + sub;
				subreddits.Add(sub);
				Karuta.Write("Added " + sub);
				string subs = "";
				foreach(string s in subreddits)
				{
					if (subs == "")
						subs += s;
					else
						subs += "|" + s;
				}
				Karuta.registry.SetValue("reddit_subs", subs);
				needsReBuild = true;
			}
		}


		//Start find and download the images
		void Crawl()
		{
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
			ImageEndpoint imgEndpoint = new ImageEndpoint(imgurClient);
			AlbumEndpoint albumEndpoint = new AlbumEndpoint(imgurClient);
			while (isRunning)
			{
				try
				{
					imgCount = 0;
					subs = new List<Subreddit>();
					if(needsReBuild)
					{
						Karuta.logger.Log("Rebuilding subreddit list", name, verbose);
						if (subreddits.Count == 0)
						{
							isRunning = false;
							return;
						}
						subs.Clear();
						foreach(string s in subreddits)
						{
							if (!isRunning)
								break;
							try
							{
								subs.Add(reddit.GetSubreddit(s));
								Karuta.logger.Log("Connected to " + s, name, verbose);
							}catch(Exception e)
							{
								Karuta.logger.LogWarning("Failed to connect to subreddit: " + s + ", " + e.Message, name, verbose);
								continue;
							}
						}
						needsReBuild = false;
					}
					if (!isRunning)
						break;
					Karuta.logger.Log("Finished Building, starting crawl", name, verbose);
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
											Karuta.logger.LogWarning("Skipping \"" + p.Title + "\", file already exsits", "/r/" + sub.Name, verbose);
											continue;
										}
										Karuta.logger.Log("Saving: " + log, "/r/" + sub.Name, verbose);
										SaveImage(p, file, p.Url);
									}else if(p.Url.DnsSafeHost == "imgur.com") //Imgur in-direct link/album
									{
										string imgurID = Path.GetFileNameWithoutExtension(p.Url.AbsolutePath);
										if (p.Url.AbsolutePath.Contains("/a/")) //Save Imgur Album
										{
											try
											{
												imgurID = p.Url.AbsolutePath;
												imgurID = imgurID.Replace("/a/", "");
												imgurID = imgurID.Replace("/", "");
												if (imgurID.Length < 3)
													continue;
												Karuta.logger.Log("Saving Album: " + log, "/r/" + sub.Name, verbose);
												Karuta.logger.Log("Album ID: " + imgurID, "/r/" + sub.Name, verbose);
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
													if (File.Exists(thisFile + Path.GetExtension(image.Link)))
													{
														Karuta.logger.LogWarning("Skipping \"" + p.Title + "\", file already exsits", "/r/" + sub.Name, verbose);
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
													if (File.Exists(file + Path.GetExtension(image.Link)))
													{
														Karuta.logger.LogWarning("Skipping \"" + p.Title + "\", file already exsits", "/r/" + sub.Name, verbose);
														continue;
													}
													Karuta.logger.Log("Saving: " + log, "/r/" + sub.Name, verbose);
													SaveImage(p, file, new Uri(image.Link));
												}catch(Exception e)
												{
													Karuta.logger.LogWarning("Unable to Download " + p.Title + ", " + e.Message, "/r/" + p.SubredditName, verbose);
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
					Karuta.Write("Crawl Failed");
					isRunning = false;
				}
				if(!isRunning)
					break;
				
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
					Thread.Sleep(updateRate);
				}
			}
			Karuta.logger.Log("Crawl has ended", name, verbose);
		}

		void SaveImage(Post p, string file, Uri url)
		{
			try
			{
				data = client.DownloadData(url);
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
		public override void Close()
		{
			Karuta.logger.Log("Crawl terminated by Karuta", name);
			if (!isRunning)
				return;
			isRunning = false;
			thread.Join();
			Karuta.RemoveThread(thread);
			thread = null;
			client.Dispose();
		}
	}
}
