using Sword;
using Sword.versification;

namespace BibleLoader
{
    public static class SwordBookMetaDataExtensions
    {
        public static Canon GetCanon(this SwordBookMetaData swordBookMetaData)
        {
            string versification = swordBookMetaData.GetCetProperty(ConfigEntryType.Versification) as string;
            return CanonManager.GetCanon(versification);
        }

        public static int GetAbsoluteChapterNumber(this SwordBookMetaData swordBookMetaData, string shortBookName,
            int chapterNumber)
        {
            Canon canon = swordBookMetaData.GetCanon();
            int chapterNumberIndexedFromZero = chapterNumber - 1;
            CanonBookDef book = canon.BookByShortName[shortBookName];
            return book.VersesInChapterStartIndex + chapterNumberIndexedFromZero;
        }
    }
}
