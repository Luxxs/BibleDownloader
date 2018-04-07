namespace GUI.BibleParser
{
	class VerseElement : Element
	{
		public string Text { get; set; }

		public override string ToString()
			=> $"{GetType().Name}: {Text}";
	}
}
