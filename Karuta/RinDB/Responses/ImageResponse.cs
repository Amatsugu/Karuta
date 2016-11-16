using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;
using Nancy;
using System.Drawing.Imaging;

namespace LuminousVector.Karuta.RinDB.Responses
{
	public class ImageResponse : Response
	{
		/// <summary>
		/// Byte array response
		/// </summary>
		/// <param name="body">Byte array to be the body of the response</param>
		/// <param name="contentType">Content type to use</param>
		public ImageResponse(Image image, string contentType = "image/png")
		{
			this.ContentType = contentType ?? "application/octet-stream";

			this.Contents = stream =>
			{
				//stream = File.OpenRead()
				image.Save(stream, ImageFormat.Png);
			};
		}

		public ImageResponse(string path, string contentType = "image/png")
		{
			this.ContentType = contentType ?? "application/octet-stream";

			this.Contents = stream =>
			{
				using (var f = File.OpenRead(path))
				{
					f.CopyTo(stream);
				}
			};
		}
	}
}
