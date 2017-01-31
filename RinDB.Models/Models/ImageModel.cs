using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Nancy;

namespace LuminousVector.RinDB.Models
{
	public class ImageModel
	{
		public string imgUri { get { return $"/image/dl/{id}"; } }
		public string fileUri { get; set; }
		public string thumbUri { get { return $"/image/thumb/{id}"; } }
		public string id { get; set; }
		public string name { get; set; }
		public string srcUri { get; set; }
		public long timeadded { get; set; }
		public bool isnsfw { get; set; }
		public TagModel[] tags { get; set; }
	}
}
