using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Nancy;

namespace LuminousVector.Karuta.RinDB.Models
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

		public ImageModel AddTag(string tagID)
		{
			RinDB.AddTag(id, tagID);
			return this;
		}

		public ImageModel AddTag(TagModel tag)
		{
			RinDB.AddTag(id, tag.id);
			return this;
		}

		public bool HasTag(string tagid)
		{
			return RinDB.HasTag(id, tagid);
		}

		public bool HasTag(TagModel tag)
		{
			return RinDB.HasTag(id, tag.id);
		}

		public TagModel[] GetTags()
		{
			return RinDB.GetTags(id);
		}

	}
}
