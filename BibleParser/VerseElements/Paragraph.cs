using System.Collections.Generic;
using BibleParser.VerseElements.Interfaces;

namespace BibleParser.VerseElements
{
    class ParagraphSeparator : IContentVerseElement
    {
        public List<IVerseElement> Content { get; set; }
        
        public override string ToString()
            => $"p({Content?.Count})";
    }
}
