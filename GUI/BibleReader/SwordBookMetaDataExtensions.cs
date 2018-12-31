using Sword;
using Sword.versification;

namespace GUI.BibleReader
{
    static class SwordBookMetaDataExtensions
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

        public static string GetModDrv(this SwordBookMetaData swordBookMetaData)
            => ((string) swordBookMetaData.GetProperty(ConfigEntryType.ModDrv)).ToUpper();
    }
}
