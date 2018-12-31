using System.Collections.Generic;

namespace GUI.BibleParser.VerseElements.Interfaces
{
    interface IContentVerseElement : IVerseElement
    {
        List<IVerseElement> Content { get; }
    }
}
