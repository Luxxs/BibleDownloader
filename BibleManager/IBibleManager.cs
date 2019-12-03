using System.Collections.Generic;
using System.Threading.Tasks;
using BibleParser.Dto;
using Sword;

namespace BibleManager
{
    public interface IBibleManager
    {
        Task<List<SwordBookMetaData>> DownloadBibleMetaDatasAsync();
        Task DownloadBibleAsync(SwordBookMetaData swordBookMetaData);
        Task<bool> IsBibleSavedAsync(SwordBookMetaData swordBookMetaData);
        Task<Chapter> GetChapterAsync(SwordBookMetaData swordBookMetaData, string shortBookName, int chapterNumber);
    }
}