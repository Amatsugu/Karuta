using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LuminousVector.RinDB.Models;

namespace LuminousVector.RinDB
{
	static class ModelExtensions
	{
		public static ImageModel AddTag(this ImageModel model, string tagID)
		{
			RinDB.AddTag(model.id, tagID);
			return model;
		}

		public static ImageModel AddTag(this ImageModel model, TagModel tag)
		{
			RinDB.AddTag(model.id, tag.id);
			return model;
		}

		public static bool HasTag(this ImageModel model, string tagid) => RinDB.HasTag(model.id, tagid);

		public static bool HasTag(this ImageModel model, TagModel tag) => RinDB.HasTag(model.id, tag.id);

		public static TagModel[] GetTags(this ImageModel model) => RinDB.GetTags(model.id).ToArray();
	}
}
