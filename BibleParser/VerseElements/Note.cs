using System.Collections.Generic;
using BibleParser.VerseElements.Interfaces;

namespace BibleParser.VerseElements
{
    class Note : IContentVerseElement
    {
        public string Identifier { get; set; }
        public List<IVerseElement> Content { get; } = new List<IVerseElement>();

        public override string ToString()
            => $"({Identifier})";
    }
}
