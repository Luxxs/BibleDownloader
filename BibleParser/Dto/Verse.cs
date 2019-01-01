using System.Collections.Generic;
using BibleParser.Dto.VerseElements.Interfaces;

namespace BibleParser.Dto
{
    public class Verse
    {
        public int Number { get; set; }
        public List<IVerseElement> Content { get; set; }

        public override string ToString()
            => $"{Number}:{{{string.Join("}{", Content)}}}";
    }
}
