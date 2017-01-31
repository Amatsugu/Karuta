using Nancy;
using System.IO;
using System.Net;

namespace LuminousVector.RinDB.Modules
{
	public class IndexModule : NancyModule
	{
		public IndexModule()
		{
			Get["/"] = _ => View["index", new { page = 1, query = ""}];
			Get["/{page}"] = p => View["index", new { page = (int)p.page, query = "" }];
			Get["/{page}/{query*}"] = p => View["index", new { page = (int)p.page, query = (string)System.Uri.EscapeDataString(p.query)}];
			
		}
	}
}
