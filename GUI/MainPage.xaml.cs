using GUI.BibleParser;
using GUI.BibleReader;
using Sword;
using Sword.reader;
using System.Collections.Generic;
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
            var bible = metadatas.Find(x => x.InternalName == "czecsp");
			if(!new BibleLoader(bible).IsBibleSaved())
			{
				await bibleDownloader.DownloadBookAsync(bible);
			}

			string modDrv = ((string)bible.GetProperty(ConfigEntryType.ModDrv)).ToUpper();
			if (modDrv.Equals("ZTEXT"))
			{
				var bibleLoader = new BibleLoader(bible);
				var chapterPositions = await bibleLoader.LoadVersePositionsAsync();
				string chapterHtml = await GetChapterHtmlAsync(bible, chapterPositions, "Matt", 0);
				Chapter chapter = await GetChapterAsync(bibleLoader, bible, chapterPositions, "Matt", 0);
			}
        }

        async Task<string> GetChapterHtmlAsync(SwordBookMetaData book, List<ChapterPosition> chapterPositions, string bookShortName, int chapter)
		{
			var bibleReader = new BibleZTextReader(book, chapterPositions, book.Name);
			var displaySettings = new DisplaySettings
			{
				ShowNotePositions = true
			};
			return await bibleReader.GetChapterHtmlAsync(displaySettings, bookShortName, chapter, false, true);
		}

		async Task<Chapter> GetChapterAsync(BibleLoader bibleLoader, SwordBookMetaData book, List<ChapterPosition> chapterPositions, string bookShortName, int chapter)
		{
			var bibleParser = new BibleParser.BibleParser(bibleLoader, chapterPositions);
			return await bibleParser.GetChapterAsync(bookShortName, chapter);
		}
    }
}
