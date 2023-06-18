using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using static Karaoke.ManageLyrics.LyricsLine;

namespace Karaoke;

internal static class ManageLyrics
{
    private static readonly List<LyricsLine> lyricsLines = new();
    private const string lyricsLinePattern = @"^\[(\d{0,2}:\d{0,2}.\d{0,2})\](.*)";
    private const string inLyricsLinePattern = @"<(\d{0,2}:\d{0,2}.\d{0,2})\>";
    private const int lyricsLengthPattern = 10;

    public static LyricsLine Get(double time) => lyricsLines.Where(x => x.time <= time).OrderByDescending(x => x.time).FirstOrDefault();

    public static void ReadFile(string path)
    {
        using StreamReader sr = new(path);
        Thread.CurrentThread.CurrentCulture = new CultureInfo("en-US");
        string line;
        while ((line = sr.ReadLine()) != null)
        {
            Match match = Regex.Match(line, lyricsLinePattern);
            if (match.Success)
            {
                lyricsLines.Add(new LyricsLine()
                {
                    time = TimeSpan.Parse("0:" + match.Groups[1].ToString() + "0").TotalSeconds,
                    lyrics = GetLyrics(match.Groups[2].ToString()),
                    times = GetTimes(match.Groups[2].ToString())
                });
            }
        }

        #region Local methods
        static string GetLyrics(string oneLine) => Regex.Replace(oneLine, inLyricsLinePattern, "");

        List<InLyricsLine> GetTimes(string oneLine)
        {
            List<InLyricsLine> times = new();
            int timeCounter = 0;
            Regex.Matches(oneLine, inLyricsLinePattern).Cast<Match>().ToList().ForEach(match => times.Add(new InLyricsLine()
            {
                time = TimeSpan.Parse("0:" + match.Value.TrimStart('<').TrimEnd('>')).TotalSeconds,
                location = match.Index - (timeCounter++ * lyricsLengthPattern),
            }));
            return times;
        }
        #endregion
    }

    internal struct LyricsLine
    {
        public double time;
        public string lyrics;
        public List<InLyricsLine> times;

        internal struct InLyricsLine
        {
            public double time;
            public int location;
        }
    }
}