using System.Collections.Generic;

namespace SubsDownloaderExtension
{
    public class Attributes
    {
        public string Subtitle_id { get; set; }
        public string Language { get; set; }
        public int Download_count { get; set; }
        public bool Ai_translated { get; set; }
        public List<Files> Files { get; set; }
    }
}