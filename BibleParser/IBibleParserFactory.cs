using Sword;

namespace BibleParser
{
    public interface IBibleParserFactory
    {
        IChapterParser GetChapterParser(SwordBookMetaData swordBookMetaData);
    }
}