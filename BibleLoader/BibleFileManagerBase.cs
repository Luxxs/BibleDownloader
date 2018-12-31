﻿using System.IO;
using System.IO.Compression;
using BibleLoader.Enums;
using Sword;

namespace BibleLoader
{
    public class BibleFileManagerBase
    {
        protected bool IsZipEntryDirectory(ZipArchiveEntry zipArchiveEntry)
            => zipArchiveEntry.FullName.EndsWith("\\") && !zipArchiveEntry.FullName.EndsWith("/") && zipArchiveEntry.Length != 0;

        protected string CreateVersificationFilePath(SwordBookMetaData swordBookMetaData, Testament testament)
            => GetBookPath(swordBookMetaData) + (testament == Testament.Old ? "ot" : "nt") + "." + (char)IndexingBlockType.Book + "zv";

        protected string CreateBzsFilePath(SwordBookMetaData swordBookMetaData, Testament testament)
            => GetBookPath(swordBookMetaData) + (testament == Testament.Old ? "ot" : "nt") + "." + (char)IndexingBlockType.Book + "zs";

        protected string CreateBzzFilePath(SwordBookMetaData swordBookMetaData, Testament testament)
            => GetBookPath(swordBookMetaData) + (testament == Testament.Old ? "ot" : "nt") + "." + (char)IndexingBlockType.Book + "zz";

        protected string GetBookPath(SwordBookMetaData swordBookMetaData)
            => swordBookMetaData.GetCetProperty(ConfigEntryType.ADataPath).ToString().Substring(2);
    }
}
