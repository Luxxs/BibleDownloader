using System.IO;
using System.Threading.Tasks;
using BibleLoader.Enums;
using Sword;

namespace BibleLoader.Interfaces
{
    public interface IBibleFileManager
    {
        /// <summary>
        /// Unzips bible files from Stream.
        /// </summary>
        Task SaveBibleAsync(Stream responseStream);

        /// <summary>
        /// Returs true if all bible files are present in filesystem.
        /// </summary>
        bool IsBibleSaved(SwordBookMetaData swordBookMetaData);

        Task<Stream> OpenBzsFileForReadAsync(SwordBookMetaData swordBookMetaData, Testament testament);
        Task<Stream> OpenBzzFileForReadAsync(SwordBookMetaData swordBookMetaData, Testament testament);
        Task<Stream> OpenVersificationFileForReadAsync(SwordBookMetaData swordBookMetaData, Testament testament);
    }
}