using System.Collections.Generic;
using GUI.BibleParser.VerseElements.Interfaces;

namespace GUI.BibleParser.VerseElements
{
    class Note : IContentVerseElement
    {
        public string Identifier { get; set; }
        public List<IVerseElement> Content { get; } = new List<IVerseElement>();

        public override string ToString()
            => $"({Identifier})";
    }
}
