namespace GUI.BibleParser
{
	class VerseElement
	{
		public string Text { get; set; }

		public override string ToString()
			=> $"{GetType().Name}: {Text}";
	}
}
