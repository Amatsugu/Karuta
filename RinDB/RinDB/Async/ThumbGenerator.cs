using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LuminousVector.RinDB.Models;
using static LuminousVector.Karuta.Karuta;
using System.Drawing;
using System.IO;
using System.Drawing.Imaging;
using Nancy;
using Nancy.Responses;
using System.Drawing.Drawing2D;

namespace LuminousVector.RinDB.Async
{
	public static class ThumbGenerator
	{
		public static int QUEUE_LENGTH { get { return _genQueue.Count; } }
		public static int GEN_COUNT { get { return _genCount; } }
		public static int ERROR_COUNT { get { return _failed.Count; } }
		public static readonly string DEFAULT_THUMB = $"{RinDB.VIEW_LOCATION}res/img/DefaultThumb.png";
		
		private static int _genCount = 0;
		private static Queue<ImageModel> _genQueue = new Queue<ImageModel>();
		private static List<ImageModel> _failed = new List<ImageModel>();

		private static bool _isGenerating;

		internal static void QueueThumb(ImageModel image)
		{
			_genQueue.Enqueue(image);
		}

		internal static void QueueThumb(IEnumerable<ImageModel> images)
		{
			foreach(ImageModel i in images)
				_genQueue.Enqueue(i);
		}

		static ThumbGenerator()
		{
			StartTimer("RinDB.ThumbGenerator", e =>
			{
				if (_isGenerating)
					return;
				_isGenerating = true;
				while (_genQueue.Count > 0)
					GenerateThumb(_genQueue.Dequeue());
				_isGenerating = false;
			}, 0, 5000);
		}

		internal static void Close()
		{
			StopTimer("RinDB.ThumbGenerator");
		}

		
		private static void GenerateThumb(ImageModel img)
		{
			if (img == null)
				return;
			if (!File.Exists(img.fileUri))
				return;
			string ext = Path.GetExtension(img.fileUri);
			string thumbUri = $"{RinDB.THUMB_DIR}/{img.id}{ext}";
			if (File.Exists(thumbUri))
				return;
			var start = DateTime.Now;
			try
			{
				using (Image image = Image.FromFile(img.fileUri, true))
				{
					int height = (int)(image.Height / (image.Width / 282f)), width = 282;
					var destRect = new Rectangle(0, 0, width, height);
					using (var destImage = new Bitmap(width, height))
					{
						destImage.SetResolution(image.HorizontalResolution, image.VerticalResolution);

						using (var graphics = Graphics.FromImage(destImage))
						{
							graphics.CompositingMode = CompositingMode.SourceCopy;
							graphics.CompositingQuality = CompositingQuality.HighSpeed;
							graphics.InterpolationMode = InterpolationMode.Bicubic;
							graphics.SmoothingMode = SmoothingMode.HighSpeed;
							graphics.PixelOffsetMode = PixelOffsetMode.HighSpeed;

							using (var wrapMode = new ImageAttributes())
							{
								wrapMode.SetWrapMode(WrapMode.TileFlipXY);
								graphics.DrawImage(image, destRect, 0, 0, image.Width, image.Height, GraphicsUnit.Pixel, wrapMode);
							}
						}
						destImage.Save(thumbUri);
						_genCount++;
					}
				}
			}catch(Exception e)
			{
				Write($"{e.Message} {img.fileUri}");
				_failed.Add(img);
				return;
			}
		}
	}
}
