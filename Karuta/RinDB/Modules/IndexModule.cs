using Nancy;
using System.IO;

namespace LuminousVector.Karuta.RinDB.Modules
{
	public class IndexModule : NancyModule
	{
		public IndexModule()
		{
			Get["/"] = _ => View["index"];
		}
	}
}
