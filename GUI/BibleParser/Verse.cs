﻿using GUI.BibleParser.DataObjects.Interfaces;
using System.Collections.Generic;

namespace GUI.BibleParser
{
	class Verse
	{
		public int Number { get; set; }
		public List<IVerseElement> Content { get; set; }

		public override string ToString()
			=> $"{Number}:{{{string.Join("}{", Content)}}}";
	}
}
