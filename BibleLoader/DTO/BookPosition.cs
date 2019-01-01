using System.Collections.Generic;

namespace BibleLoader.Dto
{
    class BookPosition
    {
        public long Length;
        public long StartPosition;
        public long Unused;
        public List<ChapterPosition> ChapterPositions = new List<ChapterPosition>();

        public BookPosition(long startPosition, long length, long unused)
        {
            this.StartPosition = startPosition;
            this.Length = length;
            this.Unused = unused;
        }
    }
}
