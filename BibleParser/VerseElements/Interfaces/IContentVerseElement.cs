using System.Collections.Generic;

namespace BibleParser.VerseElements.Interfaces
{
    interface IContentVerseElement : IVerseElement
    {
        List<IVerseElement> Content { get; }
    }
}
