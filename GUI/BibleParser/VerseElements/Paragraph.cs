using System.Collections.Generic;
using GUI.BibleParser.VerseElements.Interfaces;

namespace GUI.BibleParser.VerseElements
{
    class ParagraphSeparator : IContentVerseElement
    {
        public List<IVerseElement> Content { get; set; }
        
        public override string ToString()
            => $"p({Content?.Count})";
    }
}
