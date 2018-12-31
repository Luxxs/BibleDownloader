using GUI.BibleParser.VerseElements.Interfaces;

namespace GUI.BibleParser.VerseElements
{
    class TextElement : ITextVerseElement
    {
        public string Text { get; set; }

        public override string ToString()
            => $"{GetType().Name}: \"{Text}\"";
    }
}
