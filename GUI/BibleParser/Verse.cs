﻿using System.Collections.Generic;
using GUI.BibleParser.VerseElements.Interfaces;

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
