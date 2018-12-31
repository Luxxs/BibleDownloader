using BibleLoader.DTO;

namespace BibleParser
{
    public interface IChapterParser
    {
        Chapter ParseChapterAsync(int chapterNumber, byte[] chapterBytes, ChapterPosition versePositionsInChapter);
    }
}