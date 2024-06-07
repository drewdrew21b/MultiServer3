﻿namespace MultiSocks.DirtySocks.Messages
{
    public class PsetOut : AbstractMessage
    {
        public override string _Name { get => "PRES"; }
        public string? SHOW { get; set; }
        //public string? CHNG { get; set; } = "1";
        //public string? TITL { get; set; } = "";
        public string? PROD { get; set; }
        //public string? EXTR { get; set; }
        public string? STAT { get; set; }
        //public string? ATTR { get; set; }
    }
}