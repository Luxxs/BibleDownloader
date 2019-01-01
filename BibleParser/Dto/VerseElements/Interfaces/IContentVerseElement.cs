using System.Collections.Generic;

namespace BibleParser.Dto.VerseElements.Interfaces
{
    interface IContentVerseElement : IVerseElement
    {
        List<IVerseElement> Content { get; }
    }
}
