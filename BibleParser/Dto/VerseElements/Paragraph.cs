using System.Collections.Generic;
using BibleParser.Dto.VerseElements.Interfaces;

namespace BibleParser.Dto.VerseElements
{
    class ParagraphSeparator : IContentVerseElement
    {
        public List<IVerseElement> Content { get; set; }
        
        public override string ToString()
            => $"p({Content?.Count})";
    }
}
