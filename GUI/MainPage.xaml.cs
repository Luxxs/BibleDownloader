using Newtonsoft.Json;
using Sword;
using System.Collections.Generic;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using GUI.BibleParser;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace GUI
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        List<SwordBookMetaData> metadatas;
        readonly IBibleManager bibleManager;

        public MainPage()
        {
            this.InitializeComponent();
            bibleManager = new BibleManager();
        }

        private async void Button_Click(object sender, RoutedEventArgs e)
        {
            metadatas = await bibleManager.DownloadBibleMetaDatas();
            var bibleMetadata = metadatas.Find(x => x.InternalName == "czecsp");
            //Windows.Storage.ApplicationData.Current.LocalSettings.Values["SavedBookMetadatas"] = JsonConvert.SerializeObject(bibleMetadata);
            //var loadedMetadata = JsonConvert.DeserializeObject<SwordBookMetaData>((string)Windows.Storage.ApplicationData.Current.LocalSettings.Values["SavedBookMetadatas"]);
            if (!await bibleManager.IsBibleSaved(bibleMetadata))
            {
                await bibleManager.DownloadBibleAsync(bibleMetadata);
            }
            Chapter chapter = await bibleManager.GetChapterAsync(bibleMetadata, "Prov", 5);

            //string modDrv = ((string)loadedMetadata.GetProperty(ConfigEntryType.ModDrv)).ToUpper();
            //if (modDrv.Equals("ZTEXT"))
            //{
            //    var bibleLoader = new BibleLoader(loadedMetadata);
            //    var chapterPositions = await bibleLoader.LoadChapterPositionsAsync();
            //    string chapterHtml = await GetChapterHtmlAsync(loadedMetadata, chapterPositions, "Prov", 4);
            //    Chapter chapter = await GetChapterAsync(bibleLoader, loadedMetadata, chapterPositions, "Prov", 5);
            //}
        }

        //async Task<string> GetChapterHtmlAsync(SwordBookMetaData book, List<ChapterPosition> chapterPositions, string bookShortName, int chapter)
        //{
        //    var bibleReader = new BibleZTextReader(book, chapterPositions, book.Name);
        //    var displaySettings = new DisplaySettings
        //    {
        //        ShowNotePositions = true
        //    };
        //    return await bibleReader.GetChapterHtmlAsync(displaySettings, bookShortName, chapter, false, true);
        //}

        //async Task<Chapter> GetChapterAsync(BibleLoader bibleLoader, SwordBookMetaData book, List<ChapterPosition> chapterPositions, string bookShortName, int chapter)
        //{
        //    var bibleParser = new ChapterZTextParser(bibleLoader, chapterPositions);
        //    return await bibleParser.GetChapterAsync(bookShortName, chapter);
        //}
    }
}
