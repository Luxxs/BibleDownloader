using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading.Tasks;
using BibleLoader;
using BibleLoader.Enums;
using BibleLoader.Interfaces;
using Sword;

namespace BibleReaderConsole
{
    class BibleFileManager : BibleFileManagerBase, IBibleFileManager
    {
        readonly DirectoryInfo rootFolder = new DirectoryInfo("Bibles");

        /// <summary>
        /// Unzips bible files from Stream.
        /// </summary>
        public async Task SaveBibleAsync(Stream responseStream)
        {
            using (var zipStream = new ZipArchive(responseStream))
            {
                ReadOnlyCollection<ZipArchiveEntry> entries = zipStream.Entries;
                foreach (ZipArchiveEntry zipEntry in entries)
                {
                    if (!IsZipEntryDirectory(zipEntry))
                    {
                        var normalizedZipEntryPath = NormalizePath(zipEntry.FullName);
                        CreatePathInStorageIfNotExists(normalizedZipEntryPath);
                        var filePath = Path.Combine(rootFolder.FullName, normalizedZipEntryPath);
                        using (FileStream fileStream = File.Create(filePath))
                        {
                            using (Stream zipEntryStream = zipEntry.Open())
                            {
                                await zipEntryStream.CopyToAsync(fileStream);
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
                new FileInfo(GetConfPath(swordBookMetaData)).Exists,
                new FileInfo(GetVersificationFilePath(swordBookMetaData, Testament.Old)).Exists,
                new FileInfo(GetBzsFilePath(swordBookMetaData, Testament.Old)).Exists,
                new FileInfo(GetBzzFilePath(swordBookMetaData, Testament.Old)).Exists,
                new FileInfo(GetVersificationFilePath(swordBookMetaData, Testament.New)).Exists,
                new FileInfo(GetBzsFilePath(swordBookMetaData, Testament.New)).Exists,
                new FileInfo(GetBzzFilePath(swordBookMetaData, Testament.New)).Exists
            };
            return fileChecks.All(x => x);
        }

        public async Task<Stream> OpenBzsFileForReadAsync(SwordBookMetaData swordBookMetaData, Testament testament)
        {
            string filePath = GetBzsFilePath(swordBookMetaData, testament);
            return File.OpenRead(filePath);
        }

        public async Task<Stream> OpenBzzFileForReadAsync(SwordBookMetaData swordBookMetaData, Testament testament)
        {
            string filePath = GetBzzFilePath(swordBookMetaData, testament);
            return File.OpenRead(filePath);
        }

        public async Task<Stream> OpenVersificationFileForReadAsync(SwordBookMetaData swordBookMetaData, Testament testament)
        {
            string filePath = GetVersificationFilePath(swordBookMetaData, testament);
            return File.OpenRead(filePath);
        }

        void CreatePathInStorageIfNotExists(string normalizedPath)
        {
            DirectoryInfo folder = rootFolder;
            if(!folder.Exists)
                folder.Create();

            string[] directories = normalizedPath.Split(Path.DirectorySeparatorChar);
            foreach (string directory in directories.Take(directories.Length - 1))
            {
                folder = folder.GetDirectories().FirstOrDefault(p => p.Name.Equals(directory)) ??
                         folder.CreateSubdirectory(directory);
            }
        }

        string NormalizePath(string path)
            => path.Replace('/', Path.DirectorySeparatorChar);

        new string GetVersificationFilePath(SwordBookMetaData swordBookMetaData, Testament testament)
            => Path.Combine(rootFolder.FullName,
                NormalizePath(base.GetVersificationFilePath(swordBookMetaData, testament)));

        new string GetBzsFilePath(SwordBookMetaData swordBookMetaData, Testament testament)
            => Path.Combine(rootFolder.FullName,
                NormalizePath(base.GetBzsFilePath(swordBookMetaData, testament)));

        new string GetBzzFilePath(SwordBookMetaData swordBookMetaData, Testament testament)
            => Path.Combine(rootFolder.FullName,
                NormalizePath(base.GetBzzFilePath(swordBookMetaData, testament)));

        new string GetConfPath(SwordBookMetaData swordBookMetaData)
            => Path.Combine(rootFolder.FullName,
                NormalizePath(base.GetConfPath(swordBookMetaData)));
    }
}
