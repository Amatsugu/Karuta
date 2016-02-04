using System.IO;
using ProtoBuf.Meta;
using com.LuminousVector.DataStore;

namespace com.LuminousVector.Serialization
{
	public static class DataSerializer
	{
		private static RuntimeTypeModel _serializer;

		private static void Init()
		{
			_serializer = TypeModel.Create();
			_serializer.Add(typeof(Registry), true);
			_serializer.AllowParseableTypes = true;
			_serializer.AutoAddMissingTypes = true;
		}

		public static T deserializeData<T>(byte[] data)
		{
			if (_serializer == null)
				Init();
			if (data == null)
				return default(T);
			T deserializedObject = default(T);
			//byte[] bytes = Encoding.UTF8.GetBytes(data);
			using (MemoryStream m = new MemoryStream(data))
			{
				deserializedObject = (T)_serializer.Deserialize(m, null, typeof(T));
			}
			return deserializedObject;
		}



		public static byte[] serializeData<T>(T data)
		{
			if (_serializer == null)
				Init();
			byte[] bytes;
			using (MemoryStream m = new MemoryStream())
			{
				_serializer.Serialize(m, data);
				bytes = m.ToArray();
			}
			return bytes;
		}
	}
}