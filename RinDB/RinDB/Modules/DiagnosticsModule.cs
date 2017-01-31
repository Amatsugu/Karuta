using Nancy;
using LuminousVector.RinDB.Async;

namespace LuminousVector.RinDB.Modules
{
	public class DiagnosticsModule : NancyModule 
	{
		public DiagnosticsModule() : base("/status")
		{
			Get["/thumbs"] = _ =>
			{
				return "<meta http-equiv=\"refresh\" content=\"1; URL = /status/thumbs\">" + $"Queue: {ThumbGenerator.QUEUE_LENGTH} \n Errors: {ThumbGenerator.ERROR_COUNT} \n Completed: {ThumbGenerator.GEN_COUNT}" ;
			};
		}

	}
}
