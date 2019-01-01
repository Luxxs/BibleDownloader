using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using BibleLoader;
using BibleManager;
using BibleParser;
using BibleParser.Dto;
using Sword;

namespace BibleReaderConsole
{
    class Program
    {
        static void Main(string[] args)
        {
            var mainTask = LetsTryIt();
            Task.WaitAll(mainTask);
            Console.ReadKey();
        }

        static async Task LetsTryIt()
        {
            var bibleDownloader = new BibleDownloader();
            var bibleFileManager = new BibleFileManager();
            var bibleLoader = new BibleLoader.BibleLoader(bibleFileManager);
            var bibleParserFactory = new BibleParserFactory();
            IBibleManager bibleManager = new BibleManager.BibleManager(bibleDownloader, bibleFileManager, bibleLoader, bibleParserFactory);

            List<SwordBookMetaData> metadatas = await bibleManager.DownloadBibleMetaDatas();
            SwordBookMetaData bibleMetadata = metadatas.Find(x => x.InternalName == "czecsp");

            if (!await bibleManager.IsBibleSaved(bibleMetadata))
            {
                await bibleManager.DownloadBibleAsync(bibleMetadata);
            }
            Chapter chapter = await bibleManager.GetChapterAsync(bibleMetadata, "Prov", 5);

            foreach (Verse verse in chapter.Verses)
            {
                Console.WriteLine(verse.ToString());
            }
        }
    }
}
