using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LuminousVector.DataStore
{
	public class MissingRegistryFormatterException : Exception
	{
		public MissingRegistryFormatterException(Type type) : base($"No Registry formatter for the type: {type.Name}")
		{

		}
	}

	public class DuplicateRegistryFormatterException : Exception
	{
		public DuplicateRegistryFormatterException(Type type) : base($"The entry for {type.Name} already exists")
		{

		}
	}
}
