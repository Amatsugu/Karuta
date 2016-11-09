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
					return $"{(string)p.id} direct";
				}else
				{
					return Response.FromImage(Image.FromFile(img.fileUri), "image/png");
				}
			};
			Get["/thumb/{id}"] = p =>
			{
				ImageModel img = RinDB.GetImage((string)p.id);
				if (img == null)
				{
					return $"{(string)p.id} direct";
				}
				else
				{
					return Response.FromImage(GenerateThumb(img.fileUri), "image/png");
				}
			};
			Get["/{id}"] = p =>
			{
				ImageModel img = RinDB.GetImage((string)p.id);
				if (img == null)
				{
					return $"{(string)p.id} image page";
				}
				else
				{
					return View["image", img];
				}
			};
		}

		private Image GenerateThumb(string uri)
		{
			Image image = Image.FromFile(uri);
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
			return destImage;
		}
	}
}
