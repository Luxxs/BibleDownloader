using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Sword;

namespace GUI.BibleReader.Interfaces
{
    interface IBibleDownloader
    {
        Task<List<SwordBookMetaData>> DownloadBookMetadatasAsync();
        Task<Stream> DownloadBookAsync(SwordBookMetaData bookMetadata);
    }
}
