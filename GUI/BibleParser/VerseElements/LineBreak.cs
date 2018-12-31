using GUI.BibleParser.VerseElements.Interfaces;

namespace GUI.BibleParser.VerseElements
{
    class LineBreak : IVerseElement
    {
        public override string ToString()
            => "br";
    }
}
