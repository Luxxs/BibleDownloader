using System.Collections.Generic;
using BibleParser.Dto.VerseElements.Interfaces;

namespace BibleParser.Dto.VerseElements
{
    class Note : IContentVerseElement
    {
        public string Identifier { get; set; }
        public List<IVerseElement> Content { get; } = new List<IVerseElement>();

        public override string ToString()
            => $"({Identifier})";
    }
}
