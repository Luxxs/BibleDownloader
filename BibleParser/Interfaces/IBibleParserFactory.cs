using Sword;

namespace BibleParser.Interfaces
{
    public interface IBibleParserFactory
    {
        IChapterParser GetChapterParser(SwordBookMetaData swordBookMetaData);
    }
}