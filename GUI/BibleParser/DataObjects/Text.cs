using GUI.BibleParser.DataObjects.Interfaces;

namespace GUI.BibleParser.DataObjects
{
	class TextElement : IElementWithText
	{
		public string Text { get; set; }

		public override string ToString()
			=> $"{GetType().Name}: \"{Text}\"";
	}
}
