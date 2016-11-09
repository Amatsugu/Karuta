using Nancy;

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
