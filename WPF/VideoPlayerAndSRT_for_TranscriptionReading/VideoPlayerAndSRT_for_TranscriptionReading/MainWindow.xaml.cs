using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Text;
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
                return (double)vlcPlayer.SourceProvider.MediaPlayer.Time / vlcPlayer.SourceProvider.MediaPlayer.Length;
            }
            set
            {
                vlcPlayer.SourceProvider.MediaPlayer.Time = (long)(value * vlcPlayer.SourceProvider.MediaPlayer.Length);
                OnPropertyChanged();
            }
        }

        DirectoryInfo vlcLibDirectory;

        List<long> srt_time;


        int srt_current_index;
        long srt_current_time;
        long srt_next_time;
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

            if (vlcPlayer.SourceProvider.MediaPlayer.Time > srt_next_time)
            {
                UnSelectSRT(srt_current_index);
                srt_current_index++;
                SelectSRT(srt_current_index);
                srt_next_time = srt_time[srt_current_index];
            }
        }

        private void SelectSRT(int srt_current_index)
        {
            Dispatcher.BeginInvoke(() =>
            {
                lv_srt.SelectedIndex = srt_current_index;
            });
        }

        private void UnSelectSRT(int srt_current_index)
        {
            Dispatcher.BeginInvoke(() =>
            {
                lv_srt.SelectedIndex = -1;
            });
        }

        void LoadVideo_Click(object sender, RoutedEventArgs e)
        {
            var openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "Fichiers vidéo (*.mp4, *.avi)|*.mp4;*.avi|Tous les fichiers (*.*)|*.*";

            if (openFileDialog.ShowDialog() == true)
            {
                string[] options = new string[] { };
                //var options = new string[] { $"sub-file=Python 2023-09-18 09-28-53 - bases (Transcribed on 21-Sep-2023 17-38-40).srt" };

                FileInfo fi = new FileInfo(openFileDialog.FileName);
                FileInfo? fi_srt = FindSRT(fi);
                if (fi_srt != null)
                {
                    options.Append("sub-file=" + ((FileInfo)fi_srt).Name);
                }

                // VLC options can be given here. Please refer to the VLC command line documentation.
                vlcPlayer.SourceProvider.CreatePlayer(vlcLibDirectory, options);

                vlcPlayer.SourceProvider.MediaPlayer.TimeChanged += MediaPlayer_TimeChanged;
                vlcPlayer.SourceProvider.MediaPlayer.Play(fi);

                if (fi_srt != null)
                {
                    SRTFile srt = new SRTFile(fi_srt.FullName);
                    srt_time = new List<long>();
                    foreach (Subtitle sub in srt.Subtitles)
                    {
                        lv_srt.Items.Add(string.Join("\n", sub.Lines));
                        srt_time.Add(sub.EndTime.TotalMilliseconds);
                    }
                    srt_next_time = srt_time[0];
                }
            }
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
    }
}
