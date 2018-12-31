using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading.Tasks;
using Windows.Storage;
using GUI.BibleReader.Enums;
using GUI.BibleReader.Interfaces;
using Sword;

namespace GUI.BibleReader
{
    class BibleFileManager : IBibleFileManager
    {
        /// <summary>
        /// Unzips bible files from Stream.
        /// </summary>
        public async Task SaveBible(Stream responseStream)
        {
            StorageFolder rootFolder = ApplicationData.Current.LocalFolder;
            using (var zipStream = new ZipArchive(responseStream))
            {
                ReadOnlyCollection<ZipArchiveEntry> entries = zipStream.Entries;
                foreach (ZipArchiveEntry zipArchiveEntry in entries)
                {
                    if (!IsZipEntryDirectory(zipArchiveEntry))
                    {
                        await CreatePathInStorageIfNotExistsAsync(zipArchiveEntry.FullName);

                        StorageFile file = await
                            rootFolder.CreateFileAsync(
                                zipArchiveEntry.FullName.Replace("/", "\\"), CreationCollisionOption.ReplaceExisting);
                        using (Stream fileStream = await file.OpenStreamForWriteAsync())
                        {
                            var buffer = new byte[10000];
                            using (Stream zipEntryStream = zipArchiveEntry.Open())
                            {
                                int bytesRead;
                                while ((bytesRead = zipEntryStream.Read(buffer, 0, buffer.GetUpperBound(0))) > 0)
                                {
                                    fileStream.Write(buffer, 0, bytesRead);
                                }
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Returs true if all bible files are present in filesystem.
        /// </summary>
        public bool IsBibleSaved(SwordBookMetaData swordBookMetaData)
        {
            var fileChecks = new List<bool>
            {
                new FileInfo(CreateVersificationFilePath(swordBookMetaData, Testament.Old)).Exists,
                new FileInfo(CreateBzsFilePath(swordBookMetaData, Testament.Old)).Exists,
                new FileInfo(CreateBzzFilePath(swordBookMetaData, Testament.Old)).Exists,
                new FileInfo(CreateVersificationFilePath(swordBookMetaData, Testament.New)).Exists,
                new FileInfo(CreateBzsFilePath(swordBookMetaData, Testament.New)).Exists,
                new FileInfo(CreateBzzFilePath(swordBookMetaData, Testament.New)).Exists
            };
            return fileChecks.All(x => x);
        }

        public async Task<Stream> OpenBzsFileForReadAsync(SwordBookMetaData swordBookMetaData, Testament testament)
        {
            string filePath = CreateBzsFilePath(swordBookMetaData, testament);
            return await ApplicationData.Current.LocalFolder.OpenStreamForReadAsync(filePath);
        }

        public async Task<Stream> OpenBzzFileForReadAsync(SwordBookMetaData swordBookMetaData, Testament testament)
        {
            string filePath = CreateBzzFilePath(swordBookMetaData, testament);
            return await ApplicationData.Current.LocalFolder.OpenStreamForReadAsync(filePath);
        }

        public async Task<Stream> OpenVersificationFileForReadAsync(SwordBookMetaData swordBookMetaData, Testament testament)
        {
            string filePath = CreateVersificationFilePath(swordBookMetaData, testament);
            return await ApplicationData.Current.LocalFolder.OpenStreamForReadAsync(filePath);
        }

        bool IsZipEntryDirectory(ZipArchiveEntry zipArchiveEntry)
            => zipArchiveEntry.FullName.EndsWith("\\") && !zipArchiveEntry.FullName.EndsWith("/") && zipArchiveEntry.Length != 0;

        async Task CreatePathInStorageIfNotExistsAsync(string path)
        {
            StorageFolder folder = ApplicationData.Current.LocalFolder;
            string[] directories = path.Split("/\\".ToCharArray());
            directories = directories.Take(directories.Length - 1).ToArray();
            foreach (string directory in directories)
            {
                IReadOnlyList<StorageFolder> existingFolders = await folder.CreateFolderQuery().GetFoldersAsync();
                if (!existingFolders.Any(p => p.Name.Equals(directory)))
                {
                    folder = await folder.CreateFolderAsync(directory);
                }
                else
                {
                    folder = await folder.GetFolderAsync(directory);
                }
            }
        }

        string CreateVersificationFilePath(SwordBookMetaData swordBookMetaData, Testament testament)
            => GetBookPath(swordBookMetaData) + (testament == Testament.Old ? "ot" : "nt") + "." + (char)IndexingBlockType.Book + "zv";

        string CreateBzsFilePath(SwordBookMetaData swordBookMetaData, Testament testament)
            => GetBookPath(swordBookMetaData) + (testament == Testament.Old ? "ot" : "nt") + "." + (char)IndexingBlockType.Book + "zs";

        string CreateBzzFilePath(SwordBookMetaData swordBookMetaData, Testament testament)
            => GetBookPath(swordBookMetaData) + (testament == Testament.Old ? "ot" : "nt") + "." + (char)IndexingBlockType.Book + "zz";

        string GetBookPath(SwordBookMetaData swordBookMetaData)
            => swordBookMetaData.GetCetProperty(ConfigEntryType.ADataPath).ToString().Substring(2).Replace("/", "\\");
    }
}
