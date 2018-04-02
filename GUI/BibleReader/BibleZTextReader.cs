using Ionic.Zlib;
using Sword.reader;
using Sword.versification;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using Windows.Storage;

namespace GUI.BibleReader
{
    class BibleZTextReader
    {
        const string XmlPrefix = "<?xml version=\"1.0\" encoding=\"UTF-8\"?>\n<versee>";
        const string XmlPrefixIso = "<?xml version=\"1.0\" encoding=\"ISO-8859-1\"?>\n<versee>";
        const string XmlSuffix = "\n</versee>";
        public static Dictionary<string, string> FontPropertiesStartHtmlTags = new Dictionary<string, string>
        {
            { "acrostic", "<span style=\"text-shadow:1px 1px 5px white;\">" },
            { "bold", "<b>" },
            { "emphasis", "<em>" },
            { "illuminated", "<span style=\"text-shadow:1px 1px 5px white;\">" },
            { "italic", "<i>" },
            { "line-through", "<s>" },
            { "normal", "<span style=\"font-variant:normal;\">" },
            { "small-caps", "<span style=\"font-variant:small-caps;\">" },
            { "sub", "<sub>" },
            { "super", "<sup>" },
            { "underline", "<u>" }
        };

        public static Dictionary<string, string> FontPropertiesEndHtmlTags = new Dictionary<string, string>
        {
            { "acrostic", "</span>" },
            { "bold", "</b>" },
            { "emphasis", "</em>" },
            { "illuminated", "</span>" },
            { "italic", "</i>" },
            { "line-through", "</s>" },
            { "normal", "</span>" },
            { "small-caps", "</span>" },
            { "sub", "</sub>" },
            { "super", "</sup>" },
            { "underline", "</u>" }
        };

        List<ChapterPosition> chapterPositions = new List<ChapterPosition>();
        string iso2DigitLangCode;
        Canon canon;
        string bookPath;
        string bookName;

        public BibleZTextReader(List<ChapterPosition> chapterPositions, string iso2DigitLangCode, Canon canon, string bookPath, string bookName)
        {
            this.chapterPositions = chapterPositions;
            this.iso2DigitLangCode = iso2DigitLangCode;
            this.canon = canon;
            this.bookPath = bookPath;//TODO: Get rid of this (into some loader)
            this.bookName = bookName;
        }

        public async Task<string> GetChapterHtml(
            DisplaySettings displaySettings,
            string bookShortName,
            int chapterNumber,
            bool isNotesOnly,
            bool addStartFinishHtml)
        {
            if (this.chapterPositions.Count == 0)
            {
                return string.Empty;//TODO: throw exception instead
            }

            Debug.WriteLine("GetChapterHtml start");
            var book = canon.BookByShortName[bookShortName];
            var htmlChapter = new StringBuilder();
            ChapterPosition versesForChapterPositions = chapterPositions[chapterNumber + book.VersesInChapterStartIndex];
            string chapterStartHtml = string.Empty;
            string chapterEndHtml = string.Empty;
            if (addStartFinishHtml)
            {
                chapterStartHtml = "<html><body>";
                chapterEndHtml = "</body></html>";
            }

            /*
            string bookName = string.Empty;
            if (displaySettings.ShowBookName)
            {
                bookName = this.GetFullName(versesForChapterPositions.Booknum, isoLangCode);
            }*/

            bool isVerseMarking = displaySettings.ShowBookName || displaySettings.ShowChapterNumber
                                  || displaySettings.ShowVerseNumber;
            string startVerseMarking = displaySettings.SmallVerseNumbers
                                           ? "<sup>"
                                           : (isVerseMarking ? "<span class=\"strongsmorph\">(" : string.Empty);
            string stopVerseMarking = displaySettings.SmallVerseNumbers
                                          ? "</sup>"
                                          : (isVerseMarking ? ")</span>" : string.Empty);
            int noteIdentifier = 0;

            // in some commentaries, the verses repeat. Stop these repeats from comming in!
            var verseRepeatCheck = new Dictionary<long, int>();
            bool isInPoetry = false;
            byte[] chapterBuffer = await BibleLoader.GetChapterBytes(chapterNumber + book.VersesInChapterStartIndex, bookPath, chapterPositions, canon);

            // for debug
            //string xxxxxx = Encoding.UTF8.GetString(chapterBuffer, 0, chapterBuffer.Length);
            //Debug.WriteLine("RawChapter: " + xxxxxx);

            for (int i = 0; i < versesForChapterPositions.Verses.Count; i++)
            {
                VersePosition verse = versesForChapterPositions.Verses[i];
                string htmlChapterText = startVerseMarking
                                         + (displaySettings.ShowBookName ? bookName + " " : string.Empty)
                                         + (displaySettings.ShowChapterNumber
                                                ? ((versesForChapterPositions.ChapterNumber + 1) + ":")
                                                : string.Empty)
                                         + (displaySettings.ShowVerseNumber ? (i + 1).ToString() : string.Empty)
                                         + stopVerseMarking;
                string verseTxt;
                string id = bookShortName + "_" + chapterNumber + "_" + i;
                string restartText = "<a> ";
                string startText = htmlChapterText + restartText;
                if (!verseRepeatCheck.ContainsKey(verse.StartPosition))
                {
                    verseRepeatCheck[verse.StartPosition] = 0;

                    verseTxt = "*** ERROR ***";
                    try
                    {
                        var texts = await ParseOsisText(
                            displaySettings,
                            startText,
                            restartText,
                            chapterBuffer,
                            (int)verse.StartPosition,
                            verse.Length,
                            false,//IsIsoEncoding
                            isNotesOnly,
                            false,
                            noteIdentifier,
                            isInPoetry);
                        verseTxt = texts[0];
                        isInPoetry = bool.Parse(texts[1]);
                        noteIdentifier = int.Parse(texts[2]);
                        if (isInPoetry && (i == versesForChapterPositions.Verses.Count - 1))
                        {
                            // we must end the indentations
                            verseTxt += "</blockquote>";
                        }
                    }
                    catch (Exception e)
                    {
                        Debug.WriteLine(verse.Length + ";" + verse.StartPosition + ";" + e);
                    }
                }
                else
                {
                    verseTxt = "<p>" + startText + "</p>";
                }

                // create the verse
                htmlChapter.Append(
                    (displaySettings.EachVerseNewLine ? "<p>" : string.Empty) + chapterStartHtml + verseTxt
                    + (verseTxt.Length > 0 ? (displaySettings.EachVerseNewLine ? "</a></p>" : "</a>") : string.Empty));
                chapterStartHtml = string.Empty;
            }

            htmlChapter.Append(chapterEndHtml);
            Debug.WriteLine("GetChapterHtml end");
            return htmlChapter.ToString();
        }

        public static async Task<string[]> ParseOsisText(
            DisplaySettings displaySettings,
            string chapterNumber,
            string restartText,
            byte[] xmlbytes,
            int startPos,
            int length,
            bool isIsoText,
            bool isNotesOnly,
            bool noTitles,
            int noteIdentifier,
            bool isInPoetry,
            bool isRaw = false)
        {
            var ms = new MemoryStream();
            if (isIsoText)
            {
                var xmlPrefixIsoBytes = Encoding.UTF8.GetBytes(XmlPrefixIso);
                ms.Write(xmlPrefixIsoBytes, 0, xmlPrefixIsoBytes.Length);
            }
            else
            {
                var xmlPrefixBytes = Encoding.UTF8.GetBytes(XmlPrefix);
                ms.Write(xmlPrefixBytes, 0, xmlPrefixBytes.Length);
            }

            // Some indexes are bad. make sure the startpos and length are not bad
            if (length == 0)
            {
                return new string[] { string.Empty, isInPoetry.ToString(), noteIdentifier.ToString() };
            }

            if (startPos >= xmlbytes.Length)
            {
                Debug.WriteLine("Bad startpos;" + xmlbytes.Length + ";" + startPos + ";" + length);
                return new string[] { "*** POSSIBLE ERROR IN BOOK, TEXT MISSING HERE ***", isInPoetry.ToString(), noteIdentifier.ToString() };
            }

            if (startPos + length > xmlbytes.Length)
            {
                // we can fix this
                Debug.WriteLine("Fixed bad length;" + xmlbytes.Length + ";" + startPos + ";" + length);
                length = xmlbytes.Length - startPos - 1;
                if (length == 0)
                {
                    // this might be a problem or it might not. Put some stars here anyway.
                    return new string[] { "***", isInPoetry.ToString(), noteIdentifier.ToString() };
                }
            }

            try
            {
                // try to remove null characters at the end
                for (int i = length - 1; i > startPos; i--)
                {
                    if (xmlbytes[startPos + i] == (byte)0)
                    {
                        length--;
                    }
                    else
                    {
                        break;
                    }
                }

                //unfortuneately "&c." means "etcetera" in old english
                //and "&c." really messes up html since & is a reserved word. 
                string beforeFix = System.Text.UTF8Encoding.UTF8.GetString(xmlbytes, startPos, length);
                string afterFix = beforeFix.Replace("&c.", "&amp;c.");
                if (!beforeFix.Equals(afterFix))
                {
                    var newBuff = System.Text.UTF8Encoding.UTF8.GetBytes(afterFix);
                    ms.Write(newBuff, 0, newBuff.Length);
                }
                else
                {
                    ms.Write(xmlbytes, startPos, length);
                }

                var suffixBytes = Encoding.UTF8.GetBytes(XmlSuffix);
                ms.Write(suffixBytes, 0, suffixBytes.Length);
                ms.Position = 0;

                // debug
                //byte[] buf = new byte[ms.Length]; ms.Read(buf, 0, (int)ms.Length);
                //string xxxxxx = System.Text.UTF8Encoding.UTF8.GetString(buf, 0, buf.Length);
                //System.Diagnostics.Debug.WriteLine("osisbuf: " + xxxxxx);
                //ms.Position = 0;
            }
            catch (Exception ee)
            {
                Debug.WriteLine("crashed in a strange place. err=" + ee.StackTrace);
            }

            var plainText = new StringBuilder();
            var noteText = new StringBuilder();
            var settings = new XmlReaderSettings { IgnoreWhitespace = false };

            bool isInElement = false;
            bool isInQuote = false;
            bool isInInjectionElement = false;
            bool isInTitle = false;
            bool isInScripRef = false;
            string scriptRefText = string.Empty;
            var fontStylesEnd = new List<string>();
            bool isChaptNumGiven = false;
            bool isChaptNumGivenNotes = false;
            bool isReferenceLinked = false;
            int isLastElementLineBreak = 0;
            string lemmaText = string.Empty;
            string morphText = string.Empty;
            bool isFirstNoteInText = true;

            using (XmlReader reader = XmlReader.Create(ms, settings))
            {
                try
                {
                    // Parse the file and get each of the nodes.
                    while (reader.Read())
                    {
                        if (isLastElementLineBreak >= 1)
                        {
                            if (isLastElementLineBreak >= 2)
                            {
                                isLastElementLineBreak = 0;
                            }
                            else
                            {
                                isLastElementLineBreak = 2;
                            }
                        }

                        switch (reader.NodeType)
                        {
                            case XmlNodeType.SignificantWhitespace:
                                AppendText(reader.Value, plainText, noteText, isInElement);
                                break;

                            case XmlNodeType.Whitespace:
                                AppendText(reader.Value, plainText, noteText, isInElement);
                                break;

                            case XmlNodeType.Element:
                                switch (reader.Name.ToLower())
                                {
                                    case "img":
                                    case "figure":
                                        if (reader.HasAttributes && displaySettings.GetImageUrl != null)
                                        {
                                            reader.MoveToFirstAttribute();
                                            do
                                            {
                                                if (reader.Name.ToLower().Equals("src"))
                                                {
                                                    AppendText(
                                                        "<img src=\"" + await displaySettings.GetImageUrl(reader.Value) + "\" />",
                                                        plainText,
                                                        noteText,
                                                        isInElement);
                                                    isInQuote = true;
                                                }
                                            }
                                            while (reader.MoveToNextAttribute());
                                        }

                                        break;
                                    case "cm":
                                        if (!isRaw && !displaySettings.EachVerseNewLine && isLastElementLineBreak == 0)
                                        {
                                            AppendText("<br />", plainText, noteText, isInElement);
                                            isLastElementLineBreak = 1;
                                        }

                                        break;

                                    case "lb":
                                        if (!isRaw && !displaySettings.EachVerseNewLine)
                                        {
                                            string paragraphXml = isLastElementLineBreak == 0 ? "<br />" : " ";
                                            if (reader.HasAttributes)
                                            {
                                                reader.MoveToFirstAttribute();
                                                if (reader.Name.Equals("type"))
                                                {
                                                    {
                                                        paragraphXml = reader.Value.Equals("x-end-paragraph")
                                                                           ? "</p>"
                                                                           : (reader.Value.Equals("x-begin-paragraph")
                                                                                  ? "<p>"
                                                                                  : "<br />");
                                                    }
                                                }
                                            }

                                            AppendText(paragraphXml, plainText, noteText, isInElement);
                                            isLastElementLineBreak = 1;
                                        }

                                        break;

                                    case "title":
                                        isInTitle = true;
                                        if (!(noTitles || !displaySettings.ShowHeadings) && !isRaw)
                                        {
                                            AppendText("<h3>", plainText, noteText, isInElement);
                                        }

                                        break;
                                    case "scripref":
                                        // only the contents of the scripref are interesting
                                        isInScripRef = true;

                                        break;
                                    case "reference":
                                        // Let this be igored for now
                                        //if (reader.HasAttributes)
                                        //{
                                        //    reader.MoveToFirstAttribute();
                                        //    if (reader.Name.Equals("osisRef"))
                                        //    {
                                        //        int chaptNumLoc;
                                        //        int verseNumLoc;
                                        //        string bookShortName;
                                        //        if (ConvertOsisRefToAbsoluteChaptVerse(
                                        //            reader.Value, out bookShortName, out chaptNumLoc, out verseNumLoc))
                                        //        {
                                        //            isReferenceLinked = true;
                                        //            string textId = bookShortName + "_" + chaptNumLoc + "_" + verseNumLoc;
                                        //            AppendText(
                                        //                    "</a><a class=\"normalcolor\" id=\"ID_" + textId
                                        //               + "\"  href=\"#\" onclick=\"window.external.notify('" + textId
                                        //                + "'); event.returnValue=false; return false;\" >", plainText, noteText, isInElement);
                                        //        }
                                        //    }
                                        //}

                                        //AppendText("  [", plainText, noteText, isInElement);
                                        break;

                                    case "lg":
                                        if (!isRaw && !displaySettings.EachVerseNewLine)
                                        {
                                            if (isInPoetry)
                                            {
                                                isInPoetry = false;
                                                AppendText("</blockquote>", plainText, noteText, isInElement);
                                            }
                                            else
                                            {
                                                isInPoetry = true;
                                                AppendText(
                                                    "<blockquote style=\"margin: 0 0 0 1.5em;padding 0 0 0 0;\">",
                                                    plainText,
                                                    noteText,
                                                    isInElement);
                                            }

                                            isLastElementLineBreak = 1;
                                        }

                                        break;

                                    case "l":
                                        if (!isRaw && !displaySettings.EachVerseNewLine && isLastElementLineBreak == 0)
                                        {
                                            AppendText(isInPoetry ? "<br />" : " ", plainText, noteText, isInElement);
                                            isLastElementLineBreak = 1;
                                        }

                                        break;

                                    case "fi":
                                        if (!isRaw && !isNotesOnly && displaySettings.ShowNotePositions)
                                        {
                                            if (chapterNumber.Length > 0 && !isChaptNumGiven)
                                            {
                                                plainText.Append(chapterNumber);
                                                isChaptNumGiven = true;
                                            }

                                            plainText.Append(
                                                (displaySettings.SmallVerseNumbers ? "<sup>" : string.Empty)
                                                + convertNoteNumToId(noteIdentifier)
                                                + (displaySettings.SmallVerseNumbers ? "</sup>" : string.Empty));
                                            noteIdentifier++;
                                        }

                                        if (!isChaptNumGivenNotes && !isRaw)
                                        {
                                            AppendText("<p>" + chapterNumber, plainText, noteText, isInElement);
                                            isChaptNumGivenNotes = true;
                                        }
                                        if (!isRaw && displaySettings.ShowNotePositions)
                                        {
                                            if (!isFirstNoteInText && displaySettings.AddLineBetweenNotes)
                                            {
                                                AppendText("<br />", plainText, noteText, isInElement);
                                            }
                                            isFirstNoteInText = false;
                                            AppendText(
                                                (displaySettings.SmallVerseNumbers ? "<sup>" : string.Empty)
                                                + convertNoteNumToId(noteIdentifier)
                                                + (displaySettings.SmallVerseNumbers ? "</sup>" : string.Empty), plainText, noteText, isInElement);
                                            if (isNotesOnly)
                                            {
                                                noteIdentifier++;
                                            }
                                        }
                                        AppendText("(", plainText, noteText, isInElement);
                                        isInInjectionElement = true;
                                        break;

                                    case "rf":
                                    case "note":
                                        if (!isRaw && !isNotesOnly && displaySettings.ShowNotePositions)
                                        {
                                            if (chapterNumber.Length > 0 && !isChaptNumGiven)
                                            {
                                                plainText.Append(chapterNumber);
                                                isChaptNumGiven = true;
                                            }

                                            plainText.Append(
                                                (displaySettings.SmallVerseNumbers ? "<sup>" : string.Empty)
                                                + convertNoteNumToId(noteIdentifier)
                                                + (displaySettings.SmallVerseNumbers ? "</sup>" : string.Empty));
                                            noteIdentifier++;
                                        }

                                        if (!isChaptNumGivenNotes && !isRaw)
                                        {
                                            AppendText("<p>" + chapterNumber, plainText, noteText, true);
                                            isChaptNumGivenNotes = true;
                                        }

                                        if (!isRaw && isNotesOnly && displaySettings.ShowNotePositions)
                                        {
                                            if (!isFirstNoteInText && displaySettings.AddLineBetweenNotes)
                                            {
                                                noteText.Append("<br />");
                                            }
                                            isFirstNoteInText = false;
                                            noteText.Append(
                                                (displaySettings.SmallVerseNumbers ? "<sup>" : string.Empty)
                                                + convertNoteNumToId(noteIdentifier)
                                                + (displaySettings.SmallVerseNumbers ? "</sup>" : string.Empty));
                                            noteIdentifier++;
                                        }

                                        isInElement = true;
                                        break;

                                    case "hi":
                                        if (!isRaw)
                                        {
                                            if (reader.HasAttributes)
                                            {
                                                reader.MoveToFirstAttribute();
                                                if (reader.Name.ToLower().Equals("type"))
                                                {
                                                    var fontStyle = reader.Value.ToLower();
                                                    string startText;
                                                    if (FontPropertiesStartHtmlTags.TryGetValue(fontStyle, out startText))
                                                    {
                                                        if (!isInElement && !isInInjectionElement && chapterNumber.Length > 0 && !isInTitle
                                                            && !isChaptNumGiven)
                                                        {
                                                            if (isInQuote)
                                                            {
                                                                AppendText("</span>", plainText, noteText, isInElement);
                                                            }

                                                            plainText.Append(chapterNumber);
                                                            if (isInQuote)
                                                            {
                                                                AppendText("<span class=\"christ\">", plainText, noteText, isInElement);
                                                            }

                                                            isChaptNumGiven = true;
                                                        }

                                                        AppendText(startText, plainText, noteText, isInElement);
                                                        fontStylesEnd.Add(FontPropertiesEndHtmlTags[fontStyle]);
                                                    }
                                                }
                                            }
                                        }

                                        break;

                                    case "q":
                                        if (!isRaw && !isNotesOnly)
                                        {
                                            if (reader.HasAttributes)
                                            {
                                                reader.MoveToFirstAttribute();
                                                do
                                                {
                                                    if (displaySettings.WordsOfChristRed && reader.Name.Equals("who"))
                                                    {
                                                        if (reader.Value.ToLower().Equals("jesus"))
                                                        {
                                                            AppendText(
                                                                "<span class=\"christ\">",
                                                                plainText,
                                                                noteText,
                                                                isInElement);
                                                            isInQuote = true;
                                                        }
                                                    }

                                                    if (reader.Name.Equals("marker"))
                                                    {
                                                        AppendText(reader.Value, plainText, noteText, isInElement);
                                                    }
                                                }
                                                while (reader.MoveToNextAttribute());
                                            }
                                        }

                                        break;

                                    case "w":

                                        // <w lemma="strong:G1078" morph="robinson:N-GSF">γενεσεως</w>
                                        if ((displaySettings.ShowStrongsNumbers || displaySettings.ShowMorphology)
                                            && !isRaw && !isNotesOnly)
                                        {
                                            lemmaText = string.Empty;
                                            morphText = string.Empty;
                                            if (reader.HasAttributes)
                                            {
                                                reader.MoveToFirstAttribute();

                                                do
                                                {
                                                    if (displaySettings.ShowStrongsNumbers
                                                        && reader.Name.Equals("lemma"))
                                                    {
                                                        string[] lemmas = reader.Value.Split(' ');
                                                        foreach (string lemma in lemmas)
                                                        {
                                                            if (lemma.StartsWith("strong:") || lemma.StartsWith("s:"))
                                                            {
                                                                if (!string.IsNullOrEmpty(lemmaText))
                                                                {
                                                                    lemmaText += ",";
                                                                }
                                                                var lemmaSplit = lemma.Split(':');
                                                                lemmaText +=
                                                                    "<a class=\"strongsmorph\" href=\"#\" onclick=\"window.external.notify('STRONG_"
                                                                    + lemmaSplit[1]
                                                                    + "'); event.returnValue=false; return false;\" >"
                                                                    + lemmaSplit[1].Substring(1) + "</a>";
                                                            }
                                                        }
                                                    }
                                                    else if (displaySettings.ShowMorphology
                                                             && reader.Name.Equals("morph"))
                                                    {
                                                        string[] morphs = reader.Value.Split(' ');
                                                        foreach (string morph in morphs)
                                                        {
                                                            if (morph.StartsWith("robinson:") || morph.StartsWith("m:") || morph.StartsWith("r:"))
                                                            {
                                                                var morphSplit = morph.Split(':');
                                                                string subMorph = morphSplit[1];
                                                                if (!string.IsNullOrEmpty(morphText))
                                                                {
                                                                    morphText += ",";
                                                                }

                                                                morphText +=
                                                                    "<a class=\"strongsmorph\" href=\"#\" onclick=\"window.external.notify('MORPH_"
                                                                    + subMorph
                                                                    + "'); event.returnValue=false; return false;\" >"
                                                                    + subMorph + "</a>";
                                                            }
                                                        }
                                                    }
                                                }
                                                while (reader.MoveToNextAttribute());
                                            }
                                        }

                                        break;

                                    case "versee":
                                        //AppendText(" ", plainText, noteText, isInElement);
                                        break;
                                    case "br":
                                        // if they are smart enough to have line breaks then we need to keep them.
                                        AppendText("<br />", plainText, noteText, isInElement);
                                        break;
                                    case "p":
                                        // if they are smart enough to have paragraphs then we need to keep them.
                                        AppendText("<p>", plainText, noteText, isInElement);
                                        break;
                                    default:
                                        //AppendText(" ", plainText, noteText, isInElement);
                                        //Debug.WriteLine("Element untreated: " + reader.Name);
                                        break;
                                }

                                break;

                            case XmlNodeType.Text:
                                if (!isInElement && !isInInjectionElement && chapterNumber.Length > 0 && !isInTitle && !isInScripRef
                                    && !isChaptNumGiven)
                                {
                                    if (isInQuote)
                                    {
                                        AppendText("</span>", plainText, noteText, isInElement);
                                    }

                                    plainText.Append(chapterNumber);
                                    if (isInQuote)
                                    {
                                        AppendText("<span class=\"christ\">", plainText, noteText, isInElement);
                                    }

                                    isChaptNumGiven = true;
                                }

                                string text;
                                try
                                {
                                    text = reader.Value;
                                }
                                catch (Exception e1)
                                {
                                    Debug.WriteLine("error in text: " + e1.Message);
                                    try
                                    {
                                        text = reader.Value;
                                    }
                                    catch (Exception e2)
                                    {
                                        Debug.WriteLine("second error in text: " + e2.Message);
                                        text = "*error*";
                                    }
                                }

                                if (isInScripRef)
                                {
                                    scriptRefText += text;
                                }
                                else
                                {
                                    if ((!(noTitles || !displaySettings.ShowHeadings) || !isInTitle) && text.Length > 0)
                                    {
                                        char firstChar = text[0];
                                        AppendText(/*
                                        ((!firstChar.Equals(',') && !firstChar.Equals('.') && !firstChar.Equals(':')
                                          && !firstChar.Equals(';') && !firstChar.Equals('?'))
                                             ? " "
                                             : string.Empty) +*/ text,
                                            plainText,
                                            noteText,
                                            isInElement || isInInjectionElement);
                                    }
                                }


                                break;

                            case XmlNodeType.EndElement:
                                switch (reader.Name.ToLower())
                                {
                                    case "title":
                                        if (!(noTitles || !displaySettings.ShowHeadings) && !isRaw)
                                        {
                                            AppendText("</h3>", plainText, noteText, isInElement);
                                        }

                                        isInTitle = false;
                                        break;

                                    case "reference":
                                        AppendText("] ", plainText, noteText, isInElement);
                                        if (isReferenceLinked)
                                        {
                                            AppendText("</a>" + restartText, plainText, noteText, isInElement);
                                        }

                                        isReferenceLinked = false;
                                        break;

                                    case "note":
                                        isInElement = false;
                                        break;

                                    case "hi":
                                        if (!isRaw && fontStylesEnd.Any())
                                        {
                                            string fontStyleEnd = fontStylesEnd[fontStylesEnd.Count() - 1];
                                            fontStylesEnd.RemoveAt(fontStylesEnd.Count() - 1);
                                            AppendText(fontStyleEnd, plainText, noteText, isInElement);
                                        }

                                        break;

                                    case "q":
                                        if (isInQuote)
                                        {
                                            AppendText("</span>", plainText, noteText, isInElement);
                                            isInQuote = false;
                                        }

                                        break;

                                    case "w":

                                        // <w lemma="strong:G1078" morph="robinson:N-GSF">γενεσεως</w>
                                        if ((displaySettings.ShowStrongsNumbers || displaySettings.ShowMorphology)
                                            && !isRaw && !isNotesOnly
                                            && (!string.IsNullOrEmpty(lemmaText) || !string.IsNullOrEmpty(morphText)))
                                        {
                                            plainText.Append(
                                                "</a>"
                                                + (displaySettings.SmallVerseNumbers
                                                       ? "<sub>"
                                                       : "<span class=\"strongsmorph\">(</span>"));
                                            if (!string.IsNullOrEmpty(lemmaText))
                                            {
                                                plainText.Append(lemmaText);
                                            }

                                            if (!string.IsNullOrEmpty(morphText))
                                            {
                                                plainText.Append(
                                                    (string.IsNullOrEmpty(lemmaText) ? string.Empty : ",") + morphText);
                                            }

                                            plainText.Append(
                                                (displaySettings.SmallVerseNumbers
                                                     ? "</sub>"
                                                     : "<span class=\"strongsmorph\">)</span>") + restartText);
                                            lemmaText = string.Empty;
                                            morphText = string.Empty;
                                        }

                                        //else
                                        //{
                                        //    AppendText(" ", plainText, noteText, isInElement);
                                        //}
                                        break;

                                    case "lg":
                                        if (!isRaw && !displaySettings.EachVerseNewLine)
                                        {
                                            isInPoetry = false;
                                            AppendText("</blockquote>", plainText, noteText, isInElement);
                                        }

                                        break;

                                    case "l":
                                        if (!isRaw && !displaySettings.EachVerseNewLine)
                                        {
                                            //AppendText(" ", plainText, noteText, isInElement);
                                        }

                                        break;

                                    case "scripref":
                                        // Let's ignore this for now
                                        //AppendText(GetScripRefHtmlFromRef(scriptRefText), plainText, noteText, isInElement);
                                        scriptRefText = string.Empty;
                                        isInScripRef = false;
                                        break;
                                    case "p":
                                        // if they are smart enough to have paragraphs then we need to keep them.
                                        AppendText("</p>", plainText, noteText, isInElement);
                                        break;
                                    case "versee":
                                        AppendText(" ", plainText, noteText, isInElement);
                                        break;
                                    case "img":
                                    case "figure":
                                        // nothing needed. Already handled
                                        break;
                                    default:
                                        //AppendText(" ", plainText, noteText, isInElement);
                                        //Debug.WriteLine("EndElement untreated: " + reader.Name);
                                        break;
                                }

                                break;
                        }
                    }
                }
                catch (Exception e)
                {
                    Debug.WriteLine("BibleZtextReader.parseOsisText " + e.Message);
                }
            }

            if (isNotesOnly && !isRaw)
            {
                if (noteText.Length > 0)
                {
                    noteText.Append("</p>");
                }

                return new string[] { noteText.ToString(), isInPoetry.ToString(), noteIdentifier.ToString() };
            }

            // this replace fixes a character translation problem for slanted apostrophy
            return new string[] { plainText.ToString().Replace('\x92', '\''), isInPoetry.ToString(), noteIdentifier.ToString() };
        }
        protected static void AppendText(string text, StringBuilder plainText, StringBuilder noteText, bool isInElement)
        {
            if (!isInElement)
            {
                plainText.Append(text);
            }
            else
            {
                noteText.Append(text);
            }
        }
        protected static string convertNoteNumToId(int noteIdentifier)
        {
            string base26 = string.Empty;
            do
            {
                base26 += (char)(noteIdentifier % 26 + 'a');// IntToBase24[noteIdentifier % 26];
                noteIdentifier = noteIdentifier / 26;
            }
            while (noteIdentifier > 0);

            return "(" + Reverse(base26) + ")";
        }
        public static string Reverse(string input)
        {
            char[] output = new char[input.Length];

            int forwards = 0;
            int backwards = input.Length - 1;

            do
            {
                output[forwards] = input[backwards];
                output[backwards] = input[forwards];
            } while (++forwards <= --backwards);

            return new String(output);
        }
    }
}
