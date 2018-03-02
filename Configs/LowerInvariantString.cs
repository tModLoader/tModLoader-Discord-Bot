namespace dtMLBot.Configs
{
	public struct LowerInvariantString
	{
		private string _text;

		public static implicit operator LowerInvariantString(string text)
			=> new LowerInvariantString { _text = text };

		public string GetText()
			=> _text.ToLowerInvariant().Trim();
	}
}
