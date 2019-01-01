using BibleParser.Dto.VerseElements.Interfaces;

namespace BibleParser.Dto.VerseElements
{
    class TextElement : ITextVerseElement
    {
        public string Text { get; set; }

        public override string ToString()
            => $"{GetType().Name}: \"{Text}\"";
    }
}
