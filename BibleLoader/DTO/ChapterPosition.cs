using System.Collections.Generic;

namespace BibleLoader.DTO
{
    public class ChapterPosition
    {
        public long BookStartPosition { get; set; }
        public int BookNumber { get; set; }
        public long ChapterStartPosition { get; set; }
        public int ChapterNumber { get; set; }
        public int Length { get; set; }
        public bool IsEmpty { get; set; } //TODO: Replace with !Verses.Any()

        public List<VersePosition> Verses { get; } = new List<VersePosition>();

        public ChapterPosition(long bookStartPosition, int bookNumber, long chapterStartPosition, int chapterNumber)
        {
            BookStartPosition = bookStartPosition;
            BookNumber = bookNumber;
            ChapterStartPosition = chapterStartPosition;
            ChapterNumber = chapterNumber;
        }
    }
}
