using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace VideoPlayerAndSRT_for_TranscriptionReading
{
    public partial class MainWindow : Window, INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;
        void OnPropertyChanged([CallerMemberName] string propertyName = "")
        {
            if (PropertyChanged != null) PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
        }

        public double player_val
        {
            get
            {
                if (vlcPlayer.SourceProvider.MediaPlayer == null) return 0;
                OnPropertyChanged("time_string");
                return (double)vlcPlayer.SourceProvider.MediaPlayer.Time / vlcPlayer.SourceProvider.MediaPlayer.Length;
            }
            set
            {
                vlcPlayer.SourceProvider.MediaPlayer.Time = (long)(value * vlcPlayer.SourceProvider.MediaPlayer.Length);
                OnPropertyChanged();
            }
        }
        public string time_string
        {
            get
            {
                if (vlcPlayer.SourceProvider.MediaPlayer == null) return "-";

                return TimeSpan.FromMilliseconds(vlcPlayer.SourceProvider.MediaPlayer.Time).ToString("mm':'ss'.'fff");

            }
        }

        public string total_time_string
        {
            get
            {
                if (vlcPlayer.SourceProvider.MediaPlayer == null) return "-";
                return TimeSpan.FromMilliseconds(vlcPlayer.SourceProvider.MediaPlayer.Length).ToString("mm':'ss'.'fff");
            }
        }

        DirectoryInfo vlcLibDirectory;

        List<long> srt_time_start;
        List<long> srt_time_end;

        Dictionary<long, Subs_UC> sub_time_start;
        Dictionary<long, Subs_UC> sub_time_end;

        long valEnable_prev = -1;
        long valDisable_prev = -1;

        DispatcherTimer timer;

        public MainWindow()
        {
            InitializeComponent();
            DataContext = this;
            INIT();
        }

        void INIT()
        {
            string currentDirectory = Directory.GetCurrentDirectory();
            vlcLibDirectory = new DirectoryInfo(System.IO.Path.Combine(currentDirectory, "libvlc", IntPtr.Size == 4 ? "win-x86" : "win-x64"));

            timer = new DispatcherTimer();
            timer.Interval = TimeSpan.FromSeconds(0.2);
            timer.Tick += timer_Tick;
            timer.Start();
        }

        private void timer_Tick(object? sender, EventArgs e)
        {
            //Trouver le sous titre correspondant
            if (vlcPlayer.SourceProvider.MediaPlayer == null) return;

            long valEnable = GetPassedSRTTime(vlcPlayer.SourceProvider.MediaPlayer.Time, srt_time_start);
            long valDisable = GetPassedSRTTime(vlcPlayer.SourceProvider.MediaPlayer.Time, srt_time_end);

            if (valEnable_prev != valEnable)
            {
                valEnable_prev = valEnable;
                if (valEnable != -1)
                {
                    Subs_UC._InactiveAll();
                    sub_time_start[valEnable]._SetActive();
                    //focus
                    Dispatcher.BeginInvoke(() =>
                    {
                        lv_srt.ScrollIntoView(sub_time_start[valEnable]);
                    });
                }
            }

            if (valDisable_prev != valDisable)
            {
                valDisable_prev = valDisable;
                if (valDisable != -1)
                    sub_time_end[valDisable]._Deactive();
            }
        }

        private long GetPassedSRTTime(long time, List<long> srt_time)
        {
            long t = srt_time[0];
            for (int i = 0; i < srt_time.Count; i++)
            {
                t = srt_time[i];
                if (t > time)
                    if (i > 0)
                        return srt_time[i - 1];
                    else
                        return srt_time[0];
            }
            return -1;
        }

        private void LoadVideo_Click(object sender, MouseButtonEventArgs e)
        {
            OpenVideoAndSRTFiles();
        }

        void OpenVideoAndSRTFiles()
        {
            var openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "Fichiers vidéo (*.mp4, *.avi)|*.mp4;*.avi|Tous les fichiers (*.*)|*.*";

            if (openFileDialog.ShowDialog() == true)
            {
                FileInfo fi = new FileInfo(openFileDialog.FileName);
                OpenVideoAndSRTFiles(fi);
            }
        }

        void OpenVideoAndSRTFiles(FileInfo fi)
        {
            string[] options = new string[] { };
            //var options = new string[] { $"sub-file=fichier.srt" };
            FileInfo? fi_srt = FindSRT(fi);
            if (fi_srt != null)
            {
                options.Append("sub-file=" + fi_srt.Name);
            }

            // VLC options can be given here. Please refer to the VLC command line documentation.
            vlcPlayer.SourceProvider.CreatePlayer(vlcLibDirectory, options);

            vlcPlayer.SourceProvider.MediaPlayer.TimeChanged += MediaPlayer_TimeChanged;
            vlcPlayer.SourceProvider.MediaPlayer.EndReached += MediaPlayer_EndReached;
            vlcPlayer.SourceProvider.MediaPlayer.Opening += MediaPlayer_Opening;

            vlcPlayer.SourceProvider.MediaPlayer.Play(fi);

            LoadSRTFile(fi_srt);
        }

        void LoadSRTFile(FileInfo? fi_srt)
        {
            if (fi_srt != null)
            {
                lv_srt.Items.Clear();

                SRTFile srt = new SRTFile(fi_srt.FullName);
                srt_time_start = new List<long>();
                srt_time_end = new List<long>();

                sub_time_start = new Dictionary<long, Subs_UC>();
                sub_time_end = new Dictionary<long, Subs_UC>();

                foreach (Subtitle sub in srt.Subtitles)
                {
                    Subs_UC sub_UC = new Subs_UC();
                    sub_UC._Link(this, sub);
                    lv_srt.Items.Add(sub_UC);

                    long val = sub.StartTime.TotalMilliseconds;
                    srt_time_start.Add(val);
                    if (!sub_time_start.ContainsKey(val))
                        sub_time_start.Add(val, sub_UC);

                    val = sub.EndTime.TotalMilliseconds;
                    srt_time_end.Add(val);
                    if (!sub_time_end.ContainsKey(val))
                        sub_time_end.Add(sub.EndTime.TotalMilliseconds, sub_UC);
                }
            }
        }

        private void MediaPlayer_Opening(object? sender, Vlc.DotNet.Core.VlcMediaPlayerOpeningEventArgs e)
        {
            OnPropertyChanged("total_time_string");
            cts = new CancellationTokenSource();
            Task.Run(() => CheckIfVideoEnded());
            SetButtonPause();
        }

        FileInfo? FindSRT(FileInfo fi)
        {
            string fichiersource = fi.Name.Substring(0, fi.Name.Length - fi.Extension.Length);
            FileInfo[] fis = fi.Directory.GetFiles($"*{fichiersource}*");
            if (fis.Length == 0)
                return null;
            return fis[0];
        }

        private void MediaPlayer_TimeChanged(object? sender, Vlc.DotNet.Core.VlcMediaPlayerTimeChangedEventArgs e)
        {
            OnPropertyChanged("player_val");
        }

        private void MediaPlayer_EndReached(object? sender, Vlc.DotNet.Core.VlcMediaPlayerEndReachedEventArgs e)
        {
            SetButtonPlay();
            timer.Stop();
            VideoEndedFlag = true;
        }

        CancellationTokenSource cts;
        bool VideoEndedFlag = false;
        //gestion de l'erreur entre l'évènement VLC_EndReached et l'appel direct à Stop() 
        private async void CheckIfVideoEnded()
        {
            try
            {
                while (!cts.Token.IsCancellationRequested)
                {
                    if (VideoEndedFlag)
                    {
                        vlcPlayer.SourceProvider.MediaPlayer.Stop();
                        OnPropertyChanged("player_val");
                        VideoEndedFlag = false;

                    }
                    // Check every second
                    await Task.Delay(1000, cts.Token);
                }
            }
            catch (Exception)
            {
                // Do nothing
            }
        }

        void SetButtonPlay() { SetButtonVisible(true); }
        void SetButtonPause() { SetButtonVisible(false); }

        void SetButtonVisible(bool visible) { Dispatcher.BeginInvoke(() => { btn_play_pause.Visibility = visible ? Visibility.Hidden : Visibility.Visible; }); }


        internal void _SubsGoTo(Subtitle sub)
        {
            vlcPlayer.SourceProvider.MediaPlayer.Time = sub.StartTime.TotalMilliseconds;
        }

        private void btn_play_pause_Click(object sender, MouseButtonEventArgs e)
        {
            if (vlcPlayer.SourceProvider.MediaPlayer.IsPlaying())
            {
                //on veut faire pause
                vlcPlayer.SourceProvider.MediaPlayer.Pause();
                SetButtonPlay();
            }
            else
            {
                //on veut faire play
                vlcPlayer.SourceProvider.MediaPlayer.Play();
                SetButtonPause();
            }
        }



        private void vlc_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left && e.ClickCount == 2)
            {
                if (this.WindowState == WindowState.Maximized)
                    this.WindowState = WindowState.Normal;
                else
                    this.WindowState = WindowState.Maximized;
            }
        }

        private void Sub_edit_Click(object sender, RoutedEventArgs e)
        {

        }
    }
}
