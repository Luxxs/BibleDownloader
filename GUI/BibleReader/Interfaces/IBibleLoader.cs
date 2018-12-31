using System.Collections.Generic;
using System.Threading.Tasks;
using GUI.BibleReader.DTO;
using Sword;

namespace GUI.BibleReader.Interfaces
{
    interface IBibleLoader
    {
        Task<List<ChapterPosition>> LoadChapterPositionsAsync(SwordBookMetaData swordBookMetaData);
        Task<byte[]> GetChapterBytesAsync(SwordBookMetaData swordBookMetaData, int absoluteChapterNumber, List<ChapterPosition> chapterPositions);
    }
}