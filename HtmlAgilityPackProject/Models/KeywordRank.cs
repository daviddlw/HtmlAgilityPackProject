using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace HtmlAgilityPackProject.Models
{
    public class KeywordRank
    {
        public string DisplayUrl { get; set; }

        public string CreativeTitle { get; set; }

        public string CreativeDesc { get; set; }

        public int Rank { get; set; }

        public PositionEnum Position { get; set; }
    }

    public enum PositionEnum
    {
        Left = 1,

        Right
    }
}