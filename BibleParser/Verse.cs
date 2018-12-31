using System.Collections.Generic;
using BibleParser.VerseElements.Interfaces;

namespace BibleParser
{
    public class Verse
    {
        public int Number { get; set; }
        public List<IVerseElement> Content { get; set; }

        public override string ToString()
            => $"{Number}:{{{string.Join("}{", Content)}}}";
    }
}
