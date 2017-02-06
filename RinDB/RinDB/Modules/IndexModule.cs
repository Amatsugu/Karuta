using Nancy;
using LuminousVector.RinDB.Models;
using System.IO;
using System.Net;

namespace LuminousVector.RinDB.Modules
{
	public class IndexModule : NancyModule
	{
		public IndexModule()
		{
			Get["/"] = _ => View["index", new { page = 1, query = "", user = UserStateModel.DEFAULT }];
			int i;
			Get["/{page}"] = p => View["index", new { page = (int.TryParse((string)p.page, out i) ? i : 1), query = "", user = UserStateModel.DEFAULT }];
			Get["/{page}/{query}"] = p => View["index", new { page = (int)p.page, query = (string)System.Uri.EscapeDataString(p.query), user = UserStateModel.DEFAULT }];
			
		}
	}
}
