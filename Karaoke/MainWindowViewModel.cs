using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Karaoke.Properties;
using NAudio.Wave;
using System;
using System.Diagnostics;
using System.Reflection;
using System.Windows;
using System.Windows.Threading;

namespace Karaoke;

internal partial class MainWindowViewModel : ObservableObject
{
    #region Fields
    private readonly DispatcherTimer timer;
    private AudioFileReader reader;
    private WaveOut waveOut;
    private bool dragging = true;
    #endregion

    #region Constructor
    public MainWindowViewModel()
    {
        timer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(250)
        };
        timer.Tick += TimerTick;


        #region Local methods
        void TimerTick(object sender, EventArgs e)
        {
            TimeSpan currentTime = reader.CurrentTime;
            CurrentTime = currentTime.ToString()[3..8];

            dragging = false;
            SliderValue = currentTime.TotalSeconds;
            dragging = true;

            Lyrics = ManageLyrics.Get(currentTime.TotalSeconds);
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
            SetProperty(ref sliderValue, value);
            if (dragging && reader != null && waveOut != null && waveOut.PlaybackState != PlaybackState.Stopped)
            {
                reader.CurrentTime = TimeSpan.FromSeconds(sliderValue);
            }
        }
    }
    public Visibility ButtonsStartVisibility => WaveOutIsNotPlaying() ? Visibility.Visible : Visibility.Collapsed;
    public Visibility ButtonsPauseVisibility => WaveOutIsPlaying() ? Visibility.Visible : Visibility.Collapsed;
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
