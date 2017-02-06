using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LuminousVector.DataStore
{
	internal interface IRegistryFormatter
	{
		
	}

	public struct RegistryFormatter<T> : IRegistryFormatter
	{
		private Func<T, string> converter;
		private Func<string, T> reverter;

		public RegistryFormatter(Func<T, string> convert, Func<string, T> revert)
		{
			converter = convert;
			reverter = revert;
		}

		public string Convert(T input)
		{
			return converter.Invoke(input);
		}

		public T Revert(string input)
		{
			return reverter.Invoke(input);
		}
	}
}
