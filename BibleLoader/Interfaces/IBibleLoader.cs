using System.Collections.Generic;
using System.Threading.Tasks;
using BibleLoader.Dto;
using Sword;

namespace BibleLoader.Interfaces
{
    public interface IBibleLoader
    {
        Task<List<ChapterPosition>> LoadChapterPositionsAsync(SwordBookMetaData swordBookMetaData);
        Task<byte[]> GetChapterBytesAsync(SwordBookMetaData swordBookMetaData, int absoluteChapterNumber, List<ChapterPosition> chapterPositions);
    }
}