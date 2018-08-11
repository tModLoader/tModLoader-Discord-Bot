using System;
using System.Collections.Generic;

namespace tModloaderDiscordBot.Configs
{
	public sealed class KeyValTag : ICloneable, IEquatable<KeyValTag>
	{
		public ulong OwnerId;
		public readonly ISet<ulong> Editors = new HashSet<ulong>();
		public string Key;
		public string Value;
		public ulong LastEditor;
		public bool IsGlobal;

		public bool IsOwner(ulong id)
			=> OwnerId == id;

		public bool IsEditor(ulong id)
			=> IsOwner(id) || Editors.Contains(id);

		public bool GiveEditor(ulong id)
			=> Editors.Add(id);

		public bool TakeEditor(ulong id)
			=> Editors.Remove(id);

		public object Clone()
		{
			var clone = (KeyValTag)MemberwiseClone();
			clone.OwnerId = OwnerId;
			clone.Editors.UnionWith(Editors);
			clone.Key = Key;
			clone.Value = Value;
			clone.LastEditor = LastEditor;
			clone.IsGlobal = IsGlobal;
			return clone;
		}

		public bool Equals(KeyValTag other)
		{
			if (ReferenceEquals(null, other)) return false;
			if (ReferenceEquals(this, other)) return true;
			return OwnerId == other.OwnerId
				   && Equals(Editors, other.Editors)
				   && string.Equals(Key, other.Key)
				   && string.Equals(Value, other.Value)
				   && LastEditor == other.LastEditor
				   && IsGlobal == other.IsGlobal;
		}

		public override bool Equals(object obj)
		{
			if (ReferenceEquals(null, obj)) return false;
			if (ReferenceEquals(this, obj)) return true;
			return obj is KeyValTag tag && Equals(tag);
		}

		public override int GetHashCode()
		{
			var hashCode = -1363886798;
			hashCode = hashCode * -1521134295 + OwnerId.GetHashCode();
			hashCode = hashCode * -1521134295 + EqualityComparer<ISet<ulong>>.Default.GetHashCode(Editors);
			hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(Key);
			hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(Value);
			hashCode = hashCode * -1521134295 + LastEditor.GetHashCode();
			hashCode = hashCode * -1521134295 + IsGlobal.GetHashCode();
			return hashCode;
		}

		public static bool operator ==(KeyValTag lhs, KeyValTag rhs)
		{
			return EqualityComparer<KeyValTag>.Default.Equals(lhs, rhs);
		}

		public static bool operator !=(KeyValTag lhs, KeyValTag rhs)
		{
			return !(lhs == rhs);
		}
	}
}
