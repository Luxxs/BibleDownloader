using BibleParser.VerseElements.Interfaces;

namespace BibleParser.VerseElements
{
    class Title : ITextVerseElement
    {
        public string Text { get; set; }

        public override string ToString()
            => $"{GetType().Name}: \"{Text}\"";
    }
}
