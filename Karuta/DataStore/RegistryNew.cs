using System;
using System.Collections.Generic;
using LuminousVector.Serialization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using ProtoBuf;

namespace LuminousVector.DataStore.New
{
	[ProtoContract]
	public class RegistryNew
	{
		[ProtoMember(1)]
		private Dictionary<string, string> stringStore;
		[ProtoMember(2)]
		private Dictionary<string, int> intStore;
		[ProtoMember(3)]
		private Dictionary<string, bool> boolStore;
		[ProtoMember(4)]
		private Dictionary<string, float> floatStore;
		[ProtoIgnore]
		private Dictionary<string, IRegistryEntry> objectStore;
		[ProtoIgnore]
		private Dictionary<Type, IRegistryFormatter> objectFormatters;

		public RegistryNew() { }

		public void Init()
		{
			stringStore = new Dictionary<string, string>();
			intStore = new Dictionary<string, int>();
			boolStore = new Dictionary<string, bool>();
			floatStore = new Dictionary<string, float>();
			objectStore = new Dictionary<string, IRegistryEntry>();
		}

		public T GetValue<T>(string id)
		{
			IRegistryEntry obj;
			if(!objectStore.TryGetValue(id, out obj))
			{
				return default(T);
			}
			return ((RegistryEntry<T>)obj).value;
		}

		public void AddFormatter<T>(RegistryFormatter<T> formatter)
		{
			Type t = typeof(RegistryEntry<T>);
			if (!objectFormatters.ContainsKey(t))
				objectFormatters.Add(t, formatter);
			else
				throw new DuplicateRegistryFormatterException(typeof(RegistryFormatter<T>));
		}

		public void SetValue<T>(string id, T value, bool overwrite = true)
		{
			Type t = typeof(RegistryEntry<T>);
			if (!objectFormatters.ContainsKey(t))
				throw new MissingRegistryFormatterException(typeof(T));
			if (objectStore.ContainsKey(id) && overwrite)
				objectStore[id] = new RegistryEntry<T>(id, value, (RegistryFormatter<T>)objectFormatters[t]);
			else
				objectStore.Add(id, new RegistryEntry<T>(id, value, (RegistryFormatter<T>)objectFormatters[t]));
		}

		public void Save(string path)
		{
			foreach(var e in objectStore)
			{
				stringStore.Add(e.Key, e.Value.Convert());
			}
			File.WriteAllBytes(path, DataSerializer.serializeData(path));
		}

		public static RegistryNew Load(string path)
		{
			return DataSerializer.deserializeData<RegistryNew>(File.ReadAllBytes(path));
		}

		/*public void Migrate()
		{
			if (objectStore == null)
				objectStore = new Dictionary<string, object>();
			if (stringStore != null)
			{
				foreach (KeyValuePair<string, string> kp in stringStore)
				{
					objectStore.Add(kp.Key, kp.Value);
				}
			}
			if (intStore != null)
			{
				foreach (KeyValuePair<string, int> kp in intStore)
				{
					objectStore.Add(kp.Key, kp.Value);
				}
			}
			if (boolStore != null)
			{
				foreach (KeyValuePair<string, bool> kp in boolStore)
				{
					objectStore.Add(kp.Key, kp.Value);
				}
			}
			if (floatStore != null)
			{
				foreach (KeyValuePair<string, float> kp in floatStore)
				{
					objectStore.Add(kp.Key, kp.Value);
				}
			}
		}*/

		/*public string GetString(string id)
		{
			string value;
			if (stringStore == null)
				return null;
			stringStore.TryGetValue(id, out value);
			if (value == null)
				value = "";
			return value;
		}

		public int? GetInt(string id)
		{
			int value;
			if (intStore == null)
				return null;
			if (!intStore.TryGetValue(id, out value))
				return null;
			else
				return value;
		}

		public bool? GetBool(string id)
		{
			bool value;
			if (boolStore == null)
				return null;
			if (!boolStore.TryGetValue(id, out value))
				return null;
			else
				return value;
		}

		public float? GetFloat(string id)
		{
			float value;
			if (floatStore == null)
				return null;
			floatStore.TryGetValue(id, out value);
			return value;
		}

		public void SetValue(string id, string value)
		{
			if (stringStore == null)
				stringStore = new Dictionary<string, string>();
			if (stringStore.ContainsKey(id))
				stringStore[id] = value;
			else
				stringStore.Add(id, value);
		}

		public void SetValue(string id, int value)
		{
			if (intStore == null)
				intStore = new Dictionary<string, int>();
			if (intStore.ContainsKey(id))
				intStore[id] = value;
			else
				intStore.Add(id, value);
		}

		public void SetValue(string id, bool value)
		{
			if (boolStore == null)
				boolStore = new Dictionary<string, bool>();
			if (boolStore.ContainsKey(id))
				boolStore[id] = value;
			else
				boolStore.Add(id, value);
		}

		public void SetValue(string id, float value)
		{
			if (floatStore == null)
				floatStore = new Dictionary<string, float>();
			if (floatStore.ContainsKey(id))
				floatStore[id] = value;
			else
				floatStore.Add(id, value);
		}*/

		public void RemoveEntry(string id)
		{
			objectStore.Remove(id);
			/*if (typeof(T) == typeof(string))
			{
				stringStore.Remove(id);
			}
			else if (typeof(T) == typeof(int))
			{
				intStore.Remove(id);
			}
			else if (typeof(T) == typeof(bool))
			{
				boolStore.Remove(id);
			}
			else if (typeof(T) == typeof(float))
			{
				floatStore.Remove(id);
			}*/
		}
	}
}
