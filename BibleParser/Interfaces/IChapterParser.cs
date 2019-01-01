using BibleLoader.Dto;
using BibleParser.Dto;

namespace BibleParser.Interfaces
{
    public interface IChapterParser
    {
        Chapter ParseChapterAsync(int chapterNumber, byte[] chapterBytes, ChapterPosition versePositionsInChapter);
    }
}