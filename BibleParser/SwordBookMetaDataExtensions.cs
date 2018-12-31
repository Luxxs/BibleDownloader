using Sword;

namespace BibleParser
{
    static class SwordBookMetaDataExtensions
    {
        public static string GetModDrv(this SwordBookMetaData swordBookMetaData)
            => ((string) swordBookMetaData.GetProperty(ConfigEntryType.ModDrv)).ToUpper();
    }
}
