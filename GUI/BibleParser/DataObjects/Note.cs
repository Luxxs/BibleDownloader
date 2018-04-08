using GUI.BibleParser.DataObjects.Interfaces;
using System.Collections.Generic;

namespace GUI.BibleParser.DataObjects
{
	class Note : IElementWithContent
	{
		public string Identifier { get; set; }
		public List<IVerseElement> Content { get; } = new List<IVerseElement>();

		public override string ToString()
			=> $"({Identifier})";
	}
}
