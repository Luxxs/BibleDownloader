using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using BibleLoader;
using BibleLoader.DTO;
using BibleLoader.Interfaces;
using BibleParser;
using Sword;

namespace BibleManager
{
    public class BibleManager : IBibleManager
    {
        readonly IBibleDownloader bibleDownloader;
        readonly IBibleFileManager bibleFileManager;
        readonly IBibleLoader bibleLoader;
        readonly IBibleParserFactory bibleParserFactory;

        readonly Dictionary<SwordBookMetaData, List<ChapterPosition>> chapterPositionsCache =
            new Dictionary<SwordBookMetaData, List<ChapterPosition>>();

        public BibleManager(IBibleDownloader bibleDownloader, IBibleFileManager bibleFileManager, IBibleLoader bibleLoader, IBibleParserFactory bibleParserFactory)
        {
            this.bibleDownloader = bibleDownloader;
            this.bibleFileManager = bibleFileManager;
            this.bibleLoader = bibleLoader;
            this.bibleParserFactory = bibleParserFactory;
        }

        public async Task<List<SwordBookMetaData>> DownloadBibleMetaDatas()
            => await bibleDownloader.DownloadBookMetadatasAsync();

        public async Task DownloadBibleAsync(SwordBookMetaData swordBookMetaData)
        {
            Stream downloadedBible = await bibleDownloader.DownloadBookAsync(swordBookMetaData);
            await bibleFileManager.SaveBible(downloadedBible);
        }

        public async Task<bool> IsBibleSaved(SwordBookMetaData swordBookMetaData)
            => bibleFileManager.IsBibleSaved(swordBookMetaData);

        public async Task<Chapter> GetChapterAsync(SwordBookMetaData swordBookMetaData, string shortBookName, int chapterNumber)
        {
            int absoluteChapterNumber = swordBookMetaData.GetAbsoluteChapterNumber(shortBookName, chapterNumber);
            List<ChapterPosition> chapterPositions = await GetChapterPositionsAsync(swordBookMetaData);
            byte[] chapterBytes = await bibleLoader.GetChapterBytesAsync(swordBookMetaData, absoluteChapterNumber, chapterPositions);
            ChapterPosition versePositionsInChapter = chapterPositions[absoluteChapterNumber];
            var chapterParser = bibleParserFactory.GetChapterParser(swordBookMetaData);
            return chapterParser.ParseChapterAsync(chapterNumber, chapterBytes, versePositionsInChapter);
        }

        async Task<List<ChapterPosition>> GetChapterPositionsAsync(SwordBookMetaData swordBookMetaData)
        {
            if (!chapterPositionsCache.ContainsKey(swordBookMetaData))
            {
                List<ChapterPosition> chapterPositions = await bibleLoader.LoadChapterPositionsAsync(swordBookMetaData);
                chapterPositionsCache.Add(swordBookMetaData, chapterPositions);
            }

            return chapterPositionsCache[swordBookMetaData];
        }
    }
}
