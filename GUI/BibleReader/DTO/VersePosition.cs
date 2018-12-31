namespace GUI.BibleReader.DTO
{
    struct VersePosition
    {
        public int BookNumber { get; set; }
        public int Length { get; set; }
        public long StartPosition { get; set; }

        public VersePosition(long startPosition, int length, int bookNumber)
        {
            StartPosition = startPosition;
            Length = length;
            BookNumber = bookNumber;
        }

        public bool Equals(VersePosition equalsTo)
        {
            return equalsTo.BookNumber == BookNumber && equalsTo.Length == Length
                   && equalsTo.StartPosition == StartPosition;
        }
    }
}
