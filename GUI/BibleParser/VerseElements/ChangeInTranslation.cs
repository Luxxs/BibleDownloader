using GUI.BibleParser.VerseElements.Interfaces;

namespace GUI.BibleParser.VerseElements
{
    class ChangeInTranslation : ITextVerseElement
    {
        public string Text { get; set; }
        public string Type { get; set; }

        public override string ToString()
            => $"{GetType().Name}: \"{Type}\",\"{Text}\"";
    }
}
