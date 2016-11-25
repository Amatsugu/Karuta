using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Nancy;
using LuminousVector.Karuta.RinDB.Models;

namespace LuminousVector.Karuta.RinDB.Modules
{
	public class DBModule : NancyModule
	{
		public DBModule() : base("/DB")
		{
			Get["/latest/{count}/{page}"] = p =>
			{
				return RinDB.GetLatest((int)p.count, (int)p.page);
			};

			Get["/search/{query}/{count}/{page}"] = p =>
			{
				return RinDB.GetSearch((string)p.query, (int)p.count, (int)p.page);
			};

			Get["/tags/description/{tag}"] = p =>
			{
				return RinDB.GetTag(((string)p.tag).ToBase60()).description;
			};
		}
	}
}
