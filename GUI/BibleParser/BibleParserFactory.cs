using System;
using System.Collections.Generic;
using GUI.BibleReader;
using Sword;

namespace GUI.BibleParser
{
    class BibleParserFactory : IBibleParserFactory
    {
        readonly Dictionary<string, IChapterParser> chapterParsers = new Dictionary<string, IChapterParser>
        {
            { "ZTEXT", new ChapterZTextParser() }
        };

        public IChapterParser GetChapterParser(SwordBookMetaData swordBookMetaData)
        {
            string bibleFormat = swordBookMetaData.GetModDrv();
            if (!chapterParsers.ContainsKey(bibleFormat))
                throw new ArgumentException($"There is no parser for such format ('{bibleFormat}').");

            return chapterParsers[bibleFormat];
        }
    }
}
