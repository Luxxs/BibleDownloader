using GUI.BibleReader;
using Sword;
using Sword.reader;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace GUI
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        List<SwordBookMetaData> metadatas;

        public MainPage()
        {
            this.InitializeComponent();
        }

        private async void Button_Click(object sender, RoutedEventArgs e)
        {
			var bibleDownloader = new BibleDownloader();
			metadatas = await bibleDownloader.DownloadBookMetadatasAsync();
            var bibleKralicka = metadatas.Find(x => x.InternalName == "czebkr");
            await bibleDownloader.DownloadBookAsync(bibleKralicka);
            string genesis1 = await ReadChapterAsync(bibleKralicka.InternalName, "Matt", 0);
        }

        async Task<string> ReadChapterAsync(string bibleCodeName, string bookShortName, int chapter)
        {
            var book = metadatas.Find(x => x.InternalName == bibleCodeName);
            string modDrv = ((string)book.GetProperty(ConfigEntryType.ModDrv)).ToUpper();
            if (modDrv.Equals("ZTEXT"))
            {
                var bibleLoader = new BibleLoader(book);
                var oldTestamentChapterPositions = await bibleLoader.LoadVersePositionsAsync(Testament.Old);
                var newTestamentChapterPositions = await bibleLoader.LoadVersePositionsAsync(Testament.New);
                var chapterPositions = oldTestamentChapterPositions.Concat(newTestamentChapterPositions).ToList();
                var bibleReader = new BibleZTextReader(book, chapterPositions, book.Name);
                return await bibleReader.GetChapterHtmlAsync(new DisplaySettings(), bookShortName, chapter, false, true);
            }
            return "Unknown modDrv";
        }
    }
}
