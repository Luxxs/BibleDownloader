using System;
using System.Collections.Generic;
using BibleParser.Interfaces;
using Sword;

namespace BibleParser
{
    public class BibleParserFactory : IBibleParserFactory
    {
        readonly Dictionary<string, IChapterParser> chapterParsers = new Dictionary<string, IChapterParser>
        {
            { "ZTEXT", new ChapterZTextParser() }
        };

        public IChapterParser GetChapterParser(SwordBookMetaData swordBookMetaData)
        {
            string bibleFormat = swordBookMetaData.GetModDrv();
            if (!chapterParsers.ContainsKey(bibleFormat))
                throw new ArgumentException($"There is no parser for '{bibleFormat}' format.");

            return chapterParsers[bibleFormat];
        }
    }
}
