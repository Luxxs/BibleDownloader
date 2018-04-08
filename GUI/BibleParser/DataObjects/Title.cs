using GUI.BibleParser.DataObjects.Interfaces;

namespace GUI.BibleParser.DataObjects
{
	class Title : IElementWithText
	{
		public string Text { get; set; }

		public override string ToString()
			=> $"{GetType().Name}: \"{Text}\"";
	}
}
