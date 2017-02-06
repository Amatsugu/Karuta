using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using LuminousVector.Serialization;
using ProtoBuf;

namespace LuminousVector.DataStore
{
	[ProtoContract]
	public class Registry
	{
		[ProtoMember(1)]
		private Dictionary<string, string> stringStore;
		[ProtoMember(2)]
		private Dictionary<string, int> intStore;
		[ProtoMember(3)]
		private Dictionary<string, bool> boolStore;
		[ProtoMember(4)]
		private Dictionary<string, float> floatStore;

		public Registry() { }

		public void Init()
		{
			stringStore = new Dictionary<string, string>();
			intStore = new Dictionary<string, int>();
			boolStore = new Dictionary<string, bool>();
			floatStore = new Dictionary<string, float>();
		}

		public void Save(string path)
		{
			File.WriteAllBytes(path, DataSerializer.serializeData(this));
		}

		public static Registry Load(string path)
		{
			return DataSerializer.deserializeData<Registry>(File.ReadAllBytes(path));
		}


		/*public T GetValue<T>(string id)
		{
			object obj;
			if (!objectStore.TryGetValue(id, out obj))
			{
				return default(T);
			}
			return (T)obj;
		}*/

		/*public void SetValue<T>(string id, T value, bool overwrite = true)
		{
			if (objectStore.ContainsKey(id) && overwrite)
				objectStore[id] = value;
			else
				objectStore.Add(id, value);
		}*/

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

		public string GetString(string id)
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
		}

		/*public void RemoveEntry(string id)
		{
			//objectStore.Remove(id);
			if (typeof(T) == typeof(string))
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
			}
		}*/
	}
}
