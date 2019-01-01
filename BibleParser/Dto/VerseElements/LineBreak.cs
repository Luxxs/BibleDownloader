using BibleParser.Dto.VerseElements.Interfaces;

namespace BibleParser.Dto.VerseElements
{
    class LineBreak : IVerseElement
    {
        public override string ToString()
            => "br";
    }
}
