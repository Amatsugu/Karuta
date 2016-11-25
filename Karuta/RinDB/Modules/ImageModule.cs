using System;
using System.IO;
using System.Media;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Nancy;
using Npgsql;
using LuminousVector.Karuta.RinDB.Models;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;

namespace LuminousVector.Karuta.RinDB.Modules
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
					return Response.FromImage(Image.FromFile(img.fileUri, true), (Path.GetExtension(img.fileUri) == ".gif") ? "image/gif" : "image/png");
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
			if(!File.Exists($"{RinDB.BASE_DIR}/RinDB/thumbs/{image.id}{ext}"))
			{
				if (!Directory.Exists(RinDB.THUMB_DIR))
					Directory.CreateDirectory(RinDB.THUMB_DIR);
				return Response.FromImage(GenerateThumb(image), (ext == ".gif") ? "image/gif" : "image/png");
			}
			return Response.FromImage(Image.FromFile($"{RinDB.THUMB_DIR}/{image.id}{ext}", true), (ext == ".gif") ? "image/gif" : "image/png");
		}

		private Image GenerateThumb(ImageModel img)
		{
			if (!File.Exists(img.fileUri))
				return null;
			string ext = Path.GetExtension(img.fileUri);
			Image image = Image.FromFile(img.fileUri, true);
			int height = (int)(image.Height / (image.Width/282f)), width = 282;
			var destRect = new Rectangle(0, 0, width, height);
			var destImage = new Bitmap(width, height);

			destImage.SetResolution(image.HorizontalResolution, image.VerticalResolution);

			using (var graphics = Graphics.FromImage(destImage))
			{
				graphics.CompositingMode = CompositingMode.SourceCopy;
				graphics.CompositingQuality = CompositingQuality.HighSpeed;
				graphics.InterpolationMode = InterpolationMode.Default;
				graphics.SmoothingMode = SmoothingMode.HighSpeed;
				graphics.PixelOffsetMode = PixelOffsetMode.HighSpeed;

				using (var wrapMode = new ImageAttributes())
				{
					wrapMode.SetWrapMode(WrapMode.TileFlipXY);
					graphics.DrawImage(image, destRect, 0, 0, image.Width, image.Height, GraphicsUnit.Pixel, wrapMode);
				}
			}
			destImage.Save($"{RinDB.THUMB_DIR}/{img.id}{ext}", (ext == ".gif") ? ImageFormat.Gif : ImageFormat.Png);
			return destImage;
		}
	}
}
