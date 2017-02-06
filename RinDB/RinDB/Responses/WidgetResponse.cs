using System.Collections.Generic;
using System.Text;
using LuminousVector.RinDB.Models;
using System.IO;
using Nancy;

namespace LuminousVector.RinDB.Responses
{
	public class WidgetResponse : Response
	{
		public WidgetResponse(IEnumerable<WidgetModel> widgets)
		{
			this.ContentType = "text/HTML";

			string output = "";
			if (widgets != null)
			{
				foreach (WidgetModel W in widgets)
				{
					output += $"<div class=\"widget\">< div class=\"name\">{W.name}</div><div class=\"value\">{W.value}</div></div>";
				}
			}
			this.Contents = stream =>
			{
				using (var writer = new BinaryWriter(stream))
				{
					var data = Encoding.UTF8.GetBytes(output);
					try
					{
						writer.Write(data, 0, data.Length);
					}
					catch
					{

					}
				}
			};
		}
	}
}
