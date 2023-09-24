﻿using Microsoft.Win32.SafeHandles;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Windows.Shapes;

namespace AudioVolumeAmplitudeGraph
{
    public class Title
    {
        public int index;
        public string author;
        public string album;
        public string titleraw;

        public string titre
        {
            get
            {
                string _titre = "";
                if (author != null && author != "")
                    _titre += author + " - ";

                if (album != null && album != "")
                    _titre += album + " - ";

                _titre += titleraw;
                return _titre;
            }
        }

        public TimeSpan start;
        public TimeSpan end
        {
            get { return _end; }
            set { _end = value;
                totalTime = end - start;
            }
        }
        TimeSpan _end;

        public TimeSpan totalTime;
        public string fileName;
        public string fullFileName;
        internal object rectangle;
        internal SolidColorBrush brush;
        internal Title_UC uc;

        public Title(TimeSpan start, TimeSpan end)
        {
            this.start = start;
            this.end = end;
        }

        public void SetTitle(string chaine, string folder)
        {
            titleraw = chaine;

            string t = "";
            if (author != null && author != "")
                t += author + " - ";

            if (album != null && album != "")
                t += album + " - ";

            t += index.ToString("00") + " - ";

            t += titleraw;

            t = t.Replace("\\", "_");
            t = t.Replace("/", "_");
            t = t.Replace(":", "_");
            t = t.Replace("*", "_");
            t = t.Replace("?", "_");
            t = t.Replace("\"", "_");
            t = t.Replace("<", "_");
            t = t.Replace(">", "_");
            t = t.Replace("|", "_");

            fileName = t + ".mp3";

            fullFileName = folder + "\\" + fileName;
        }

        public override string ToString()
        {
            return titre + " [" + totalTime.ToString("mm\\:ss") + "]";
        }
    }
}
