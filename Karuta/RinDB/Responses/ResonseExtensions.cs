using Nancy;
using System.Collections.Generic;
using System.Drawing;
using LuminousVector.Karuta.RinDB.Models;
using LuminousVector.Karuta.RinDB.Responses;

namespace LuminousVector.Karuta.RinDB
{
	static class ResonseExtensions
	{
		public static Response FromByteArray(this IResponseFormatter formatter, byte[] body, string contentType = null)
		{
			return new ByteArrayResponse(body, contentType);
		}

		public static Response FromImage(this IResponseFormatter formatter, Image image, string contentType = "image/png")
		{
			return new ImageResponse(image, contentType);
		}

		public static Response FromImage(this IResponseFormatter formatter, string image, string contentType = "image/png")
		{
			return new ImageResponse(image, contentType);
		}

		public static Response AsHTML(this IResponseFormatter formatter, IEnumerable<ImageModel> images, string contentType = "text/html")
		{
			return new ImageModelHtmlResponse(images, contentType);
		}

		public static Response FromImageModel(this IResponseFormatter formatter, ImageModel image)
		{
			return null;
		}
	}
}
