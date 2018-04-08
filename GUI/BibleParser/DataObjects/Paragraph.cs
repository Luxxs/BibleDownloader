using GUI.BibleParser.DataObjects.Interfaces;
using System.Collections.Generic;

namespace GUI.BibleParser.DataObjects
{
	class ParagraphSeparator : IElementWithContent
	{
		public List<IVerseElement> Content { get; set; }
		
		public override string ToString()
			=> $"p({Content?.Count})";
	}
}
