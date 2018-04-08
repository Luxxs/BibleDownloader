using System.Collections.Generic;

namespace GUI.BibleParser
{
	class ParagraphSeparator : VerseElement, IElementWithContent
	{
		public List<VerseElement> Content { get; set; }
	}
}
