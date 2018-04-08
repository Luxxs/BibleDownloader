using System.Collections.Generic;

namespace GUI.BibleParser.DataObjects.Interfaces
{
	interface IElementWithContent : IVerseElement
	{
		List<IVerseElement> Content { get; }
	}
}
