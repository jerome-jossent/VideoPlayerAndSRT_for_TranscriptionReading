using NAudio_JJ;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.Json;
using static NAudio_JJ.Peak;
using System.Windows.Media;

namespace AudioVolumeAmplitudeGraph
{
    public class VolumeAnalysis
    {
        public string filename;
        public DATA data;

        double _silence_detection_sensitivity;
        double _silence_detection_mintime;
        public event PeakAnalysingHandler peakAnalysingEvent;


        public VolumeAnalysis() { }


        void GetInfo(bool usejsoninstead)
        {
            //reset data
            data = new DATA();

            //Read musiquefile
            data.totaltime = NAudio_JJ.NAudio_JJ.MusicTotalSeconds(filename);

            //get Peaks Amplitude
            string jsonfile = AppDomain.CurrentDomain.BaseDirectory + @"json.tmp";
            if (!System.IO.File.Exists(jsonfile))
                usejsoninstead = false;

            string path = filename;

            if (usejsoninstead)
            {
                data.peaks = Peak.Get_Peaks_FromJson(jsonfile);
            }
            else
            {
                Peak.peakAnalysingEvent += peakAnalysingEvent;
                data.peaks = Peak.Get_Peaks(path);
                Peak.peakAnalysingEvent -= peakAnalysingEvent;
                string jsonString = JsonSerializer.Serialize(data.peaks);
                System.IO.File.WriteAllText(jsonfile, jsonString);
            }
            GetInfo_2();
        }

        void GetInfo_2()
        {
            if (data == null) return;
            GetInfo_2(data.peaks, ref data.silences, ref data.titles, data.totaltime);
        }
        void GetInfo_2(List<Peak> peaks, ref List<Silence> silences, ref List<Title> titles, double totaltime_sec)
        {
            //get silences
            silences = Silence.Get_Silences(peaks, _silence_detection_sensitivity, _silence_detection_mintime);

            TracksFinder(silences, ref titles);

            //TRIM data
            //silences 1 et n
            if (silences.Count > 0 && silences[0].debut == TimeSpan.Zero.TotalSeconds)
            {
                //change piste 1
                titles[0].start = TimeSpan.FromSeconds((double)silences[0].fin);
                //delete silence 1
                silences.RemoveAt(0);
            }
            if (silences.Count > 0 && titles.Count > 1 && (double)silences[silences.Count - 1].fin >= totaltime_sec)
            {
                //change piste n
                titles[titles.Count - 1].end = TimeSpan.FromSeconds(silences[silences.Count - 1].debut);
                //delete silence n
                silences.RemoveAt(silences.Count - 1);
            }

            //List_Titles();
            //List_Silences();
            //ReDrawGraph();
        }


        
        //void Peak_peakAnalysingEvent(object sender, Peak.PeakAnalysingEventArgs e)
        //{
        //    Dispatcher.Invoke(new Action(() =>
        //    {
        //        //_progressbar.Value = e.Val; 
        //    }), DispatcherPriority.Background, null);
        //}
        

        static void TracksFinder(List<Silence> silences, ref List<Title> titles)
        {
            titles = new List<Title>();
            for (int i = 0; i < silences.Count - 1; i++)
            {
                Silence silence = silences[i];

                //first title
                if (i == 0)
                {
                    titles.Add(new Title(TimeSpan.Zero, TimeSpan.FromSeconds(silence.debut))
                    {
                        index = titles.Count + 1,
                        brush = new SolidColorBrush(GetNextColor(titles.Count + 1))
                    });
                }
                titles.Add(new Title(TimeSpan.FromSeconds((double)silence.fin),
                                     TimeSpan.FromSeconds(silences[i + 1].debut))
                {
                    index = titles.Count + 1,
                    brush = new SolidColorBrush(GetNextColor(titles.Count + 1)),
                });
            }
        }

        static List<System.Windows.Media.Color> colors = new List<System.Windows.Media.Color>(){
            System.Windows.Media.Colors.CadetBlue,
            System.Windows.Media.Colors.DarkKhaki,
            System.Windows.Media.Colors.DarkTurquoise,
            System.Windows.Media.Colors.LightBlue,
            System.Windows.Media.Colors.LightCoral,
            System.Windows.Media.Colors.LightGreen,
            System.Windows.Media.Colors.LightPink,
            System.Windows.Media.Colors.LightSalmon,
            System.Windows.Media.Colors.LightSkyBlue,
            System.Windows.Media.Colors.LimeGreen,
            System.Windows.Media.Colors.MediumOrchid,
            System.Windows.Media.Colors.Plum,
            System.Windows.Media.Colors.SandyBrown,
            System.Windows.Media.Colors.Thistle
            };


        static System.Windows.Media.Color GetNextColor(int index)
        {
            System.Windows.Media.Color c;
            while (index > colors.Count - 1) { index -= colors.Count; }
            return colors[index];
        }
    }
}
