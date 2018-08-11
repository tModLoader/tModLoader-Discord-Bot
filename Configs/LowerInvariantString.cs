using System;
using System.Collections.Generic;

namespace tModloaderDiscordBot.Configs
{
	public struct LowerInvariantString : IEquatable<LowerInvariantString>
	{
		private string _text;

		public static implicit operator LowerInvariantString(string text)
			=> new LowerInvariantString { _text = text };

		public static bool operator ==(LowerInvariantString string1, LowerInvariantString string2)
		{
			return string1.Equals(string2);
		}

		public static bool operator !=(LowerInvariantString string1, LowerInvariantString string2)
		{
			return !(string1 == string2);
		}

		public string GetText()
			=> _text.ToLowerInvariant().Trim();

		public override bool Equals(object obj)
		{
			return obj is LowerInvariantString && Equals((LowerInvariantString)obj);
		}

		public bool Equals(LowerInvariantString other)
		{
			return _text == other._text;
		}

		public override int GetHashCode()
		{
			return 1197319925 + EqualityComparer<string>.Default.GetHashCode(_text);
		}
	}
}
