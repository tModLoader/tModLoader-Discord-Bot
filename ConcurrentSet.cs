using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;

namespace tModloaderDiscordBot
{
	// TODO -> do we need concurrency? no.. ? async = 1 thread
	public sealed class ConcurrentSet<T> : ConcurrentDictionary<T, byte>
	{

		public ConcurrentSet() : base()
		{

		}

		public ConcurrentSet(ConcurrentSet<T> e) : base(e.ToArray())
		{

		}

		public ConcurrentSet(IEnumerable<T> e) : base(e.ToDictionary(x => x, y => byte.MinValue))
		{

		}

		public ConcurrentSet(IEnumerable<KeyValuePair<T, byte>> e) : base(e)
		{

		}
	}

	public class ConcurrentSetConverter<T> : JsonConverter
	{
		public override bool CanConvert(Type objectType)
		{
			return (objectType == typeof(ConcurrentSet<T>));
		}

		public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
		{
			ConcurrentSet<T> eobj = (ConcurrentSet<T>)value;
			var temp = new ConcurrentDictionary<T, byte>(eobj);
			serializer.Serialize(writer, temp);
		}

		public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
		{
			var temp = serializer.Deserialize<ConcurrentDictionary<T, byte>>(reader);
			ConcurrentSet<T> eobj = new ConcurrentSet<T>();
			foreach (var key in temp.Keys)
			{
				eobj.TryAdd(key, temp[key]);
			}
			return eobj;
		}
	}
}
