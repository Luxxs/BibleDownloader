using System.Collections.Generic;

namespace GUI.BibleParser
{
	class Verse : IElementWithContent
	{
		public int Number { get; set; }
		public List<VerseElement> Content { get; set; }

		public override string ToString()
			=> $"{{{string.Join("}{", Content)}}}";
	}
}
