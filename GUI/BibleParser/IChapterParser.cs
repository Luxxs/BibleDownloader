using GUI.BibleReader.DTO;

namespace GUI.BibleParser
{
    interface IChapterParser
    {
        Chapter ParseChapterAsync(int chapterNumber, byte[] chapterBytes, ChapterPosition versePositionsInChapter);
    }
}