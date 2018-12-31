using BibleParser.VerseElements.Interfaces;

namespace BibleParser.VerseElements
{
    class LineBreak : IVerseElement
    {
        public override string ToString()
            => "br";
    }
}
