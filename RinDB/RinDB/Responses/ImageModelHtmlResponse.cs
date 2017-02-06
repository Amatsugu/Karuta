using System;
using Nancy;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LuminousVector.RinDB.Models;

namespace LuminousVector.RinDB.Responses
{
	class ImageModelHtmlResponse : Response
	{
		public ImageModelHtmlResponse(IEnumerable<ImageModel> images, string contentType)
		{
			this.ContentType = contentType ?? "text/HTML";

			string output = "";
			if (images != null)
			{
				foreach (ImageModel I in images)
				{
					output += $"<a href=\"/image/{I.id}\" style=\"display:none;\" class=\"imageCard\"><div class=\"image\"><img src=\"{I.thumbUri}\"></div><div class=\"name\">{I.name}</div></a>";
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
					}catch
					{

					}
				}
			};
		}
	}
}
