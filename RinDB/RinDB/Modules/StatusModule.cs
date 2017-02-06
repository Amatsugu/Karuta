using Nancy;
using Nancy.Responses;
using LuminousVector.RinDB.Async;
using LuminousVector.RinDB.Models;

namespace LuminousVector.RinDB.Modules
{
	public class StatusModule : NancyModule 
	{
		public StatusModule() : base("/status")
		{
			Get["/"] = _ =>
			{
				return View["status", new
				{
					thumb = new WidgetModel[]
					{
						new WidgetModel("Queue", ThumbGenerator.QUEUE_LENGTH),
						new WidgetModel("Completed", ThumbGenerator.GEN_COUNT),
						new WidgetModel("Errors", ThumbGenerator.ERROR_COUNT),
						new WidgetModel("Is Running", ThumbGenerator.IS_RUNNING)
					},
					db = new WidgetModel[]
					{
						new WidgetModel("Queue", DBUpdateQueue.QUEUE_LENGTH),
						new WidgetModel("Completed", DBUpdateQueue.COMPLEDTED_COUNT),
						new WidgetModel("Is Running", ThumbGenerator.IS_RUNNING)
					},
					user = UserStateModel.DEFAULT
				}];
			};

			Get["/thumbs"] = _ =>
			{
				return Response.AsHTML(new WidgetModel[]
					{
						new WidgetModel("Queue", ThumbGenerator.QUEUE_LENGTH),
						new WidgetModel("Completed", ThumbGenerator.GEN_COUNT),
						new WidgetModel("Errors", ThumbGenerator.ERROR_COUNT),
						new WidgetModel("Is Running", ThumbGenerator.IS_RUNNING)
					});
			};

			Get["/db"] = _ =>
			{
				return Response.AsHTML(new WidgetModel[]
					{
						new WidgetModel("Queue", DBUpdateQueue.QUEUE_LENGTH),
						new WidgetModel("Completed", DBUpdateQueue.COMPLEDTED_COUNT),
						new WidgetModel("Is Running", ThumbGenerator.IS_RUNNING)
					});
			};
		}

	}
}
