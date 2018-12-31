using Sword;

namespace GUI.BibleParser
{
    interface IBibleParserFactory
    {
        IChapterParser GetChapterParser(SwordBookMetaData swordBookMetaData);
    }
}