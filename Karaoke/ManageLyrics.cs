namespace Karaoke
{
    internal static class ManageLyrics
    {
        public static string Get(double time)
        {
            string lyrics = "";
            if (time >= 1) lyrics = "In the town where I was born";
            if (time >= 6) lyrics = "Lived a man who sailed to sea";
            if (time >= 10.5) lyrics = "And he told us of his life";
            if (time >= 15) lyrics = "In the land of submarines";
            return lyrics;
        }
    }
}
