using System.Collections.Generic;

namespace GUI.BibleParser
{
	interface IElementWithContent
	{
		List<VerseElement> Content { get; }
	}
}
