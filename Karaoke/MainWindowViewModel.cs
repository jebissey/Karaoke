using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Karaoke.Properties;
using NAudio.Wave;
using System;
using System.Diagnostics;
using System.Reflection;
using System.Windows;
using System.Windows.Threading;
using static Karaoke.ManageLyrics;

namespace Karaoke;

internal partial class MainWindowViewModel : ObservableObject
{
    #region Fields
    private readonly DispatcherTimer timer;
    private AudioFileReader reader;
    private WaveOut waveOut;
    private bool draggingSlider = true;
    private LyricsLine? lastLyricsLine;
    #endregion

    #region Constructor
    public MainWindowViewModel()
    {
        timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(250) };
        timer.Tick += TimerTick;


        #region Local methods
        void TimerTick(object sender, EventArgs e)
        {
            TimeSpan currentTime = reader.CurrentTime;
            CurrentTime = currentTime.ToString()[3..8];

            draggingSlider = false;
            SliderValue = Math.Round(currentTime.TotalSeconds, 0);
            draggingSlider = true;

            LyricsLine lyricsLine = Get(currentTime.TotalSeconds);
            if (TheSame(lastLyricsLine ?? new(), lyricsLine)) GradientStopStart = GradientStopMiddle = GradientStopStop = 0;
            lastLyricsLine = lyricsLine;

            Lyrics = lyricsLine.lyrics ?? "";
            if (lyricsLine.times.Count > 0)
            {
                int? start = null, stop = null;
                foreach (LyricsLine.InLyricsLine time in lyricsLine.times)
                {
                    if (start == null && currentTime.TotalSeconds < time.time) return;
                    if (start == null) start = time.location;
                    else
                    {
                        if (currentTime.TotalSeconds <= time.time) stop = time.location;
                        else start = time.location;
                    }
                    if (start != null && stop != null)
                    {
                        double max = lyricsLine.lyrics.Length;
                        GradientStopStart = 1 - (max - start ?? 0) / max;
                        GradientStopStop = 1 - (max - stop ?? 0) / max;
                        GradientStopMiddle = gradientStopStart + ((gradientStopStop - gradientStopStart) / 2);
                        return;
                    }
                }
            }
            else GradientStopStart = GradientStopMiddle = GradientStopStop = 0;
        }
        #endregion
    }
    #endregion

    #region Properties
    public static string Title => $"Karaoke - V{GetSoftVersion()}";

    private string lyrics;
    public string Lyrics
    {
        get => lyrics;
        set => SetProperty(ref lyrics, value);
    }

    private string currentTime;
    public string CurrentTime
    {
        get => currentTime;
        set => SetProperty(ref currentTime, value);
    }

    private string totalTime;
    public string TotalTime
    {
        get => totalTime;
        set => SetProperty(ref totalTime, value);
    }

    private double sliderMax;
    public double SliderMax
    {
        get => sliderMax;
        set => SetProperty(ref sliderMax, value);
    }

    private double sliderValue;
    public double SliderValue
    {
        get => sliderValue;
        set
        {
            if (sliderValue != value)
            {
                SetProperty(ref sliderValue, value);
                if (draggingSlider && reader != null && waveOut != null && waveOut.PlaybackState != PlaybackState.Stopped)
                {
                    reader.CurrentTime = TimeSpan.FromSeconds(sliderValue);
                }
            }
        }
    }
    public Visibility ButtonsStartVisibility => WaveOutIsNotPlaying() ? Visibility.Visible : Visibility.Collapsed;
    public Visibility ButtonsPauseVisibility => WaveOutIsPlaying() ? Visibility.Visible : Visibility.Collapsed;


    private double gradientStopStart;
    public double GradientStopStart
    {
        get => gradientStopStart;
        set => SetProperty(ref gradientStopStart, value);
    }

    private double gradientStopMiddle;
    public double GradientStopMiddle
    {
        get => gradientStopMiddle;
        set => SetProperty(ref gradientStopMiddle, value);
    }

    private double gradientStopStop;
    public double GradientStopStop
    {
        get => gradientStopStop;
        set => SetProperty(ref gradientStopStop, value);
    }
    #endregion

    #region Relay commands
    [RelayCommand(CanExecute = "WaveOutIsNotPlaying")]
    private void Start()
    {
        if (waveOut?.PlaybackState == PlaybackState.Paused) waveOut.Play();
        else
        {
            ManageLyrics.ReadFile(Settings.Default.LyricsFile);
            reader = new(Settings.Default.MusicFile);
            waveOut = new WaveOut();
            waveOut.Init(reader);
            waveOut.Play();
            waveOut.PlaybackStopped += OutputPlaybackStopped;

            TotalTime = reader.TotalTime.ToString()[3..8];
            CurrentTime = reader.CurrentTime.ToString()[3..8];
            SliderMax = reader.TotalTime.TotalSeconds;
            timer.Start();
        }
        SetStateButtons();

        #region Local methods
        void OutputPlaybackStopped(object sender, StoppedEventArgs e) => Stop();
        #endregion
    }
    private bool WaveOutIsNotPlaying() => waveOut?.PlaybackState != PlaybackState.Playing;

    [RelayCommand(CanExecute = "WaveOutIsPlaying")]
    private void Pause()
    {
        waveOut?.Pause();
        SetStateButtons();
    }
    private bool WaveOutIsPlaying() => waveOut?.PlaybackState == PlaybackState.Playing;

    [RelayCommand(CanExecute = "WaveOutIsPlaying")]
    private void Stop()
    {
        waveOut?.Stop();

        timer.Stop();
        waveOut.Dispose();
        reader.Dispose();

        SetStateButtons();
    }
    #endregion

    #region Private methods
    private void SetStateButtons()
    {
        StartCommand.NotifyCanExecuteChanged();
        PauseCommand.NotifyCanExecuteChanged();
        StopCommand.NotifyCanExecuteChanged();
        OnPropertyChanged(nameof(ButtonsStartVisibility));
        OnPropertyChanged(nameof(ButtonsPauseVisibility));
    }

    private static string GetSoftVersion()
    {
        Assembly assembly = Assembly.GetExecutingAssembly();
        FileVersionInfo fileVersionInfo = FileVersionInfo.GetVersionInfo(assembly.Location);
        return fileVersionInfo.FileVersion ?? "";
    }
    #endregion
}
