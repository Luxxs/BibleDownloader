using System.Collections.Generic;

namespace GUI.BibleParser
{
	class Note : VerseElement, IElementWithContent
	{
		public string Identifier { get; set; }
		public List<VerseElement> Content { get; } = new List<VerseElement>();

		public override string ToString()
			=> $"({Identifier})";
	}
}
