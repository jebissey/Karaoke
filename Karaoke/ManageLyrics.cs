using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading;

namespace Karaoke;

internal static class ManageLyrics
{
    private static List<LyricsLine> lyricsLines = new();

    public static string Get(double time)
    {
        string lyrics = "";
        foreach (var lyricsLine in lyricsLines)
        {
            if (time >= lyricsLine.time) lyrics = lyricsLine.lyrics;
        }
        return lyrics;
    }

    public static void ReadFile(string path)
    {
        using (StreamReader sr = new StreamReader(path))
        {
            Thread.CurrentThread.CurrentCulture = new CultureInfo("en-US");
            string pattern = @"^\[(\d{0,2}:\d{0,2}.\d{0,2})\](.*)";
            string line;
            while ((line = sr.ReadLine()) != null)
            {
                Match match = Regex.Match(line, pattern);
                if (match.Success)
                {
                    lyricsLines.Add(new LyricsLine() { time = TimeSpan.Parse("0:" + match.Groups[1].ToString() + "0").TotalSeconds, lyrics = match.Groups[2].ToString() });
                }
            }
        }
    }

    private struct LyricsLine
    {
        public double time;
        public string lyrics;
    }
}