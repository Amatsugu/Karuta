using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Nancy;
using LuminousVector.RinDB.Models;
using LuminousVector.Utils.Extensions;

namespace LuminousVector.RinDB.Modules
{
	public class DBModule : NancyModule
	{
		public DBModule() : base("/DB")
		{
			Get["/latest/{count}/{page}"] = p =>
			{
				return Response.AsHTML(RinDB.GetLatest((int)p.count, (int)p.page));
			};

			Get["/search/{query}/{count}/{page}"] = p =>
			{
				return Response.AsHTML(RinDB.GetSearch((string)p.query, (int)p.count, (int)p.page));
			};

			Get["/tags/description/{tag}"] = p =>
			{
				return RinDB.GetTag(((string)p.tag).ToBase60()).description;
			};
		}
	}
}
