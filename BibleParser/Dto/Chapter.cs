using System.Collections.Generic;

namespace BibleParser.Dto
{
    public class Chapter
    {
        public int Number { get; set; }
        public List<Verse> Verses { get; set; }
    }
}
