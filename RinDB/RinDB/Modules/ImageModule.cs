using System;
using System.IO;
using System.Media;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Nancy;
using Npgsql;
using LuminousVector.RinDB.Models;
using LuminousVector.RinDB.Async;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;

namespace LuminousVector.RinDB.Modules
{
	public class ImageModule : NancyModule
	{
		public ImageModule() : base("/image")
		{
			
			Get["/dl/{id}"] = p =>
			{
				ImageModel img = RinDB.GetImage((string)p.id);
				if (img == null)
				{
					return $"{(string)p.id} Not found";
				}else
				{
					return Response.FromImage(img.fileUri);
				}
			};
			Get["/thumb/{id}"] = p =>
			{
				ImageModel img = RinDB.GetImage((string)p.id);
				if (img == null)
				{
					return $"{(string)p.id} Not found";
				}
				else
				{
					return GetOrGenerateThumb(img);
				}
			};
			Get["/{id}"] = p =>
			{
				ImageModel img = RinDB.GetImage((string)p.id);
				if (img == null)
				{
					return $"{(string)p.id} Not Found";
				}
				else
				{
					return View["image", img];
				}
			};
		}


		private Response GetOrGenerateThumb(ImageModel image)
		{
			string ext = Path.GetExtension(image.fileUri);
			if(!File.Exists($"{RinDB.THUMB_DIR}/{image.id}{ext}"))
			{
				if (!Directory.Exists(RinDB.THUMB_DIR))
					Directory.CreateDirectory(RinDB.THUMB_DIR);
				ThumbGenerator.QueueThumb(image);
				return Response.AsRedirect("/res/img/DefaultThumb.png");
			}
			return Response.FromImage($"{RinDB.THUMB_DIR}/{image.id}{ext}");
		}
	}
}
