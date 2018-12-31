using System.Collections.Generic;
using System.Threading.Tasks;
using BibleParser;
using Sword;

namespace BibleManager
{
    public interface IBibleManager
    {
        Task<List<SwordBookMetaData>> DownloadBibleMetaDatas();
        Task DownloadBibleAsync(SwordBookMetaData swordBookMetaData);
        Task<bool> IsBibleSaved(SwordBookMetaData swordBookMetaData);
        Task<Chapter> GetChapterAsync(SwordBookMetaData swordBookMetaData, string shortBookName, int chapterNumber);
    }
}