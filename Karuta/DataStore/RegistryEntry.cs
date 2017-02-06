using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ProtoBuf;

namespace LuminousVector.DataStore
{
	public struct RegistryEntry<T> : IRegistryEntry
	{
		public string ID;
		public T value { get; set; }
		public RegistryFormatter<T> formatter { get; set; }

		public RegistryEntry(string id, T value, RegistryFormatter<T> formatter)
		{
			this.value = value;
			ID = id;
			this.formatter = formatter;
		}

		public Type GetEntryType() => typeof(T);

		public string Convert() => formatter.Convert((T)value);

		public void Revert(string input) => value = formatter.Revert(input);
	}

	public interface IRegistryEntry
	{
		string Convert();

		void Revert(string input);

		Type GetEntryType();
	}
}
