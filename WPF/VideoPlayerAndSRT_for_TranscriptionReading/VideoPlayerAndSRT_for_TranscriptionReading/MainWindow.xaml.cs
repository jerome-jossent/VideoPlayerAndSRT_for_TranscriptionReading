﻿using Microsoft.Win32;
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

using Vlc.DotNet.Core;

namespace VideoPlayerAndSRT_for_TranscriptionReading
{
    public partial class MainWindow : Window, INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;
        void OnPropertyChanged([CallerMemberName] string propertyName = "")
        {
            if (PropertyChanged != null) PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
        }

        SRTFile srt;

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

        int srt_current_index_start = -1;
        int srt_current_index_end = -1;

        DispatcherTimer timer;

        string[] vlc_options;
        FileInfo fi_video;
        FileInfo fi_srt;
        Subs_UC subs_UC_current;

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

        long valEnable;
        long valDisable;

        private void timer_Tick(object? sender, EventArgs e)
        {
            //Trouver le sous titre correspondant
            if (vlcPlayer.SourceProvider.MediaPlayer == null) return;

            if (vlcPlayer.SourceProvider.MediaPlayer.Time > valEnable)
            {
                valEnable = GetNextSRTStartTime(vlcPlayer.SourceProvider.MediaPlayer.Time);
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

            if (vlcPlayer.SourceProvider.MediaPlayer.Time > valDisable)
            {
                valDisable = GetNextSRTEndTime(vlcPlayer.SourceProvider.MediaPlayer.Time);

                if (valDisable != -1)
                    sub_time_end[valDisable]._Deactive();
            }
        }

        long GetNextSRTStartTime(long time)
        {
            if (srt_current_index_start == -1)
            {
                srt_current_index_start = 0;
                return srt_time_start[0];
            }

            for (int i = srt_current_index_start; i < srt_time_start.Count; i++)
            {
                if (srt_time_start[i] > time)
                {
                    srt_current_index_start = i;
                    return srt_time_start[i - 1];
                }
            }
            return srt_time_start[srt_time_start.Count - 1];
        }

        long GetNextSRTEndTime(long time)
        {
            if (srt_current_index_end == -1)
            {
                srt_current_index_end = 0;
                return srt_time_end[0];
            }

            for (int i = srt_current_index_end; i < srt_time_end.Count; i++)
            {
                if (srt_time_end[i] > time)
                {
                    if (i == 0)
                        return srt_time_end[0];
                    else
                    {
                        srt_current_index_end = i;
                        return srt_time_end[i - 1];
                    }
                }
            }
            return srt_time_end[srt_time_end.Count - 1];
        }

        long GetPassedSRTTime(long time, List<long> srt_time)
        {
            long t;
            for (int i = 0; i < srt_time.Count; i++)
            {
                t = srt_time[i];
                if (t > time)
                    if (i > 0)
                    {
                        //srt_current_index = i;
                        return srt_time[i - 1];
                    }
                    else
                    {
                        //srt_current_index = 0;
                        return srt_time[0];
                    }
            }
            return srt_time[srt_time.Count - 1];
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

        void OpenVideoAndSRTFiles(FileInfo fi, FileInfo? fi_srt = null)
        {
            this.fi_srt = fi_srt;
            LoadVideoFileWithSRT(fi, fi_srt);
            LoadSRTFile();
        }


        void LoadVideoFileWithSRT(FileInfo fi, FileInfo? fi_srt = null)
        {
            fi_video = fi;
            vlc_options = new string[] { };
            //var options = new string[] { $"sub-file=fichier.srt" };
            if (fi_srt == null)
                fi_srt = FindSRT(fi_video);
            if (fi_srt != null)
            {
                vlc_options.Append("sub-file=" + fi_srt.Name);
            }

            // VLC options can be given here. Please refer to the VLC command line documentation.
            //vlcPlayer.SourceProvider.CreatePlayer(vlcLibDirectory, options);
            vlcPlayer.SourceProvider.CreatePlayer(vlcLibDirectory);

            vlcPlayer.SourceProvider.MediaPlayer.TimeChanged += MediaPlayer_TimeChanged;
            vlcPlayer.SourceProvider.MediaPlayer.EndReached += MediaPlayer_EndReached;
            vlcPlayer.SourceProvider.MediaPlayer.Opening += MediaPlayer_Opening;

            vlcPlayer.SourceProvider.MediaPlayer.Play(fi_video, vlc_options);
        }

        void ReLoadVideoFileWithSRT()
        {
            var position = vlcPlayer.SourceProvider.MediaPlayer.Position;
            vlcPlayer.SourceProvider.MediaPlayer.Play(fi_video, vlc_options);
            vlcPlayer.SourceProvider.MediaPlayer.Position = position;
        }


        void LoadSRTFile()
        {
            if (fi_srt == null) return;

            lv_srt.Items.Clear();

            srt = new SRTFile(fi_srt.FullName);
            srt_time_start = new List<long>();
            srt_time_end = new List<long>();

            sub_time_start = new Dictionary<long, Subs_UC>();
            sub_time_end = new Dictionary<long, Subs_UC>();

            foreach (Subtitle sub in srt.Subtitles)
            {
                Subs_UC sub_UC = new Subs_UC();
                sub_UC._Link(this, sub);
                lv_srt.Items.Add(sub_UC);

                long val = sub.startTime.TotalMilliseconds;
                srt_time_start.Add(val);
                if (!sub_time_start.ContainsKey(val))
                    sub_time_start.Add(val, sub_UC);

                val = sub.endTime.TotalMilliseconds;
                srt_time_end.Add(val);
                if (!sub_time_end.ContainsKey(val))
                    sub_time_end.Add(sub.endTime.TotalMilliseconds, sub_UC);
            }

            if (srt_time_start.Count == 0)
            {
                //srt fail
                valEnable = long.MaxValue;
                valDisable = long.MaxValue;
                return;
            }

            //inits
            valEnable = srt_time_start[0];
            valDisable = srt_time_end[0];
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
            vlcPlayer.SourceProvider.MediaPlayer.Time = sub.startTime.TotalMilliseconds;
        }

        void btn_play_pause_Click(object sender, MouseButtonEventArgs e)
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

        void vlc_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left && e.ClickCount == 2)
            {
                if (this.WindowState == WindowState.Maximized)
                    this.WindowState = WindowState.Normal;
                else
                    this.WindowState = WindowState.Maximized;
            }
        }


        //drag & drop files to app
        void Window_Drop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);

                FileInfo fi_0 = new FileInfo(files[0]);
                FileInfo fi_1 = null;
                if (files.Length > 1)
                    fi_1 = new FileInfo(files[1]);

                if (fi_0.Extension.ToLower() == ".srt")
                    OpenVideoAndSRTFiles(fi_1, fi_0);
                else
                    OpenVideoAndSRTFiles(fi_0, fi_1);
            }
        }

        internal void _Edit(Subs_UC subs_UC)
        {
            UnSelect();
            subs_UC_current = subs_UC;
            subs_UC._isEdited = true;
        }

        internal void _Valid(Subs_UC subs_UC)
        {
            UnSelect();

            //save new srt
            srt.Save();

            //reload SRT in video player
            ReLoadVideoFileWithSRT();
        }

        void UnSelect()
        {
            if (subs_UC_current != null)
                subs_UC_current._isEdited = false;
        }

        void btn_refresh_Click(object sender, MouseButtonEventArgs e)
        {
            LoadSRTFile();
        }
    }
}
