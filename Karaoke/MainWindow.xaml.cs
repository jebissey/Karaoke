using Karaoke.Properties;
using NAudio.Wave;
using System;
using System.Windows;
using System.Windows.Threading;

namespace reproductor;

public partial class MainWindow : Window
{

    DispatcherTimer timer;

    //Lector de archivos
    AudioFileReader reader;
    //Comunicacion con la tarjeta de audio
    //Exclusivo para salida
    WaveOut output;

    bool dragging = false;

    public MainWindow()
    {
        InitializeComponent();

        btnRerpoducir.IsEnabled = true;
        btnDetener.IsEnabled = false;
        btnPausa.IsEnabled = false;

        timer = new DispatcherTimer();
        timer.Interval = TimeSpan.FromMilliseconds(500);
        timer.Tick += Timer_Tick;

    }

    private void Timer_Tick(object sender, EventArgs e)
    {
        lblTiempoActual.Text = reader.CurrentTime.ToString().Substring(0, 8);

        if (!dragging)
        {
            sldTiempo.Value = reader.CurrentTime.TotalSeconds;
        }

        pbCancion.Value = reader.CurrentTime.TotalSeconds;


        if (pbCancion.Value >= 1)
        {
            txtLyrics.Text = "In the town where I was born";
        }

        if (pbCancion.Value >= 6)
        {
            txtLyrics.Text = "Lived a man who sailed to sea";
        }

        if (pbCancion.Value >= 10.5)
        {
            txtLyrics.Text = "And he told us of his life";
        }

        if (pbCancion.Value >= 15)
        {
            txtLyrics.Text = "In the land of submarines";
        }
    }





    private void BtnRerpoducir_Click(object sender, RoutedEventArgs e)
    {

        if (output != null && output.PlaybackState == PlaybackState.Paused)
        {
            //retomo reproduccion
            output.Play();
            btnRerpoducir.IsEnabled = true;
            btnPausa.IsEnabled = true;
            btnDetener.IsEnabled = true;
        }
        else
        {
            reader = new AudioFileReader(Settings.Default.SongFile);
            output = new WaveOut();


            output.PlaybackStopped += Output_PlaybackStopped;

            output.Init(reader);
            output.Play();

            btnRerpoducir.IsEnabled = false;
            btnPausa.IsEnabled = true;
            btnDetener.IsEnabled = true;

            lblTiempoTotal.Text = reader.TotalTime.ToString().Substring(0, 8);
            lblTiempoActual.Text = reader.CurrentTime.ToString().Substring(0, 8);

            sldTiempo.Maximum = reader.TotalTime.TotalSeconds;
            sldTiempo.Value = reader.CurrentTime.TotalSeconds;

            pbCancion.Maximum = reader.TotalTime.TotalSeconds;

            timer.Start();

        }
    }

    private void Output_PlaybackStopped(object sender, StoppedEventArgs e)
    {
        timer.Stop();
        reader.Dispose();
        output.Dispose();
    }



    private void BtnDetener_Click(object sender, RoutedEventArgs e)
    {
        if (output != null)
        {
            output.Stop();
            btnRerpoducir.IsEnabled = true;
            btnPausa.IsEnabled = false;
            btnDetener.IsEnabled = false;
        }
    }

    private void BtnPausa_Click(object sender, RoutedEventArgs e)
    {
        if (output != null)
        {
            output.Pause();
            btnRerpoducir.IsEnabled = true;
            btnPausa.IsEnabled = false;
            btnDetener.IsEnabled = true;
        }
    }

    private void SldTiempo_DragStarted(object sender, System.Windows.Controls.Primitives.DragStartedEventArgs e)
    {
        dragging = true;
    }

    private void SldTiempo_DragCompleted(object sender, System.Windows.Controls.Primitives.DragCompletedEventArgs e)
    {
        dragging = false;

        if (reader != null && output != null && output.PlaybackState != PlaybackState.Stopped)
        {
            reader.CurrentTime = TimeSpan.FromSeconds(sldTiempo.Value);
        }
    }
}
