using Nancy;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using LuminousVector.RinDB.Models;
using LuminousVector.RinDB.Responses;

namespace LuminousVector.RinDB
{
	static class ResonseExtensions
	{
		public static Response FromByteArray(this IResponseFormatter formatter, byte[] body, string contentType = null)
		{
			return new ByteArrayResponse(body, contentType);
		}

		public static Response FromImage(this IResponseFormatter formatter, string path, string contentType = "image/png")
		{
			switch(System.IO.Path.GetExtension(path))
			{
				case ".jpg":
					contentType = "image/jpeg";
					break;
				case ".jpeg":
					contentType = "image/jpeg0";
					break;
				case ".gif":
					contentType = "image/gif";
					break;
			}
			return new ImageResponse(path, contentType);
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
