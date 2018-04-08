using GUI.BibleParser.DataObjects.Interfaces;

namespace GUI.BibleParser.DataObjects
{
	class ChangeInTranslation : IElementWithText
	{
		public string Text { get; set; }
		public string Type { get; set; }

		public override string ToString()
			=> $"{GetType().Name}: \"{Type}\",\"{Text}\"";
	}
}
