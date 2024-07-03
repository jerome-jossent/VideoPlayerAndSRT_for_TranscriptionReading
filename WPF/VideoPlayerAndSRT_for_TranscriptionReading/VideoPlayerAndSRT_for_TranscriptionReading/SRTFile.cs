using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Shapes;


//https://github.com/iivmok/srtlib.net/blob/master/srtlib.net.cs

namespace VideoPlayerAndSRT_for_TranscriptionReading
{
    public class SRTFile
    {
        private static Regex rxHTML = new Regex(@"<(.|\n)*?>", RegexOptions.Compiled);

        string pathSRT;

        /// <summary>
        /// List of all subtitles in the file.
        /// </summary>
        public List<Subtitle> Subtitles = new List<Subtitle>();

        /// <summary>
        /// Get the EndTime of the last subtitle.
        /// </summary>
        public TimeSpan LastEndTime
        {
            get
            {
                TimeSpan time = TimeSpan.Zero;
                foreach (var item in Subtitles)
                {
                    if (item.endTime > time) time = item.endTime;
                }
                return time;
            }
        }

        /// <summary>
        /// Get all subtitles that are valid at a specific time.
        /// </summary>
        /// <param name="time"></param>
        /// <returns></returns>
        public List<Subtitle> GetSubtitlesAt(TimeSpan time)
        {
            List<Subtitle> result = new List<Subtitle>();
            foreach (var item in Subtitles)
            {
                if (time > item.startTime && time < item.endTime)
                {
                    result.Add(item);
                }
            }
            return result;
        }

        /// <summary>
        /// Parses an .srt file.
        /// </summary>
        /// <param name="strSRTFile">Path to the file</param>
        /// <param name="enc">Encoding to use when reading the .srt file. By default uses UTF-8.</param>
        /// <param name="stripHTMLTags">Strip HTML tags from the subtitles. Default is false.</param>
        public SRTFile(string pathSRT, Encoding enc = null, bool stripHTMLTags = false)
        {
            if (pathSRT == null)
                throw new ArgumentNullException("pathSRT");

            if (enc == null)
                enc = Encoding.UTF8;
         
            this.pathSRT = pathSRT;

            string[] lines = Regex.Replace(File.ReadAllText(pathSRT), "\r\n?", "\n").Split('\n');

            int error_line = 0;
            int state = 0;
            bool wasEmptyLine = false;

            Subtitle currentSubtitle = null;
            try
            {
                for (int i = 0; i < lines.Length; i++)
                {
                    error_line = i;
                    int seqNumber;
                    bool isLineNumber = int.TryParse(lines[i], out seqNumber);

                    if (isLineNumber && wasEmptyLine)
                    {
                        state = 0;
                        wasEmptyLine = false;

                        Subtitles.Add(currentSubtitle);
                        currentSubtitle = null;
                    }
                    switch (state)
                    {
                        case 0: //beginning of a new subtitle line
                            if (lines[i] != "")
                            {
                                currentSubtitle = new Subtitle(seqNumber);
                                state++;
                            }
                            break;
                        case 1: //after the subtitle number
                            currentSubtitle.ParseTime(lines[i]);
                            state++;
                            break;
                        case 2: //text lines or an empty line after srt, before number
                            if (lines[i] != "")
                            {
                                if (stripHTMLTags)
                                    currentSubtitle.Lines_Add(StripHTML(lines[i]));
                                else
                                    currentSubtitle.Lines_Add(lines[i]);
                            }
                            else
                            {
                                wasEmptyLine = true;
                            }
                            break;
                    }
                }
                if (currentSubtitle != null)
                    Subtitles.Add(currentSubtitle);
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show(ex.ToString() + string.Format("\r\n\r\nError line: {0}", error_line));
                throw new Exception("Invalid SRT file.", ex);
            }
        }

        private static string StripHTML(string htmlString)
        {
            return rxHTML.Replace(htmlString, string.Empty);
        }

        /// <summary>
        /// Renders this object to a .srt file format.
        /// </summary>
        /// <returns>string containing the contents of the whole file</returns>
        public string Render()
        {
            StringBuilder sb = new StringBuilder();

            for (int i = 0; i < Subtitles.Count; i++)            
                sb.Append(Subtitles[i].ToString());            

            return sb.ToString();
        }

        /// <summary>
        /// Writes the render of this object to specified path.
        /// </summary>
        public void WriteToFile(string path)
        {
            File.WriteAllText(path, Render());
        }

        internal void Save()
        {
            WriteToFile(pathSRT);
        }

        /// <summary>
        /// Multiply the times of this object, for example to convert to other FPS.
        /// </summary>
        public void Multiply(double factor)
        {
            foreach (var item in Subtitles)
            {
                item.Multiply(factor);
            }
        }

        /// <summary>
        /// Offset the times of this object by a number of milliseconds.
        /// </summary>
        public void Add(int ms)
        {
            foreach (var item in Subtitles)
            {
                item.Add(ms);
            }
        }
    }

    public class Subtitle
    {
        public int sequenceNumber;
        public SRTTime startTime, endTime;
        public List<string> lines = new List<string>();
        public List<string> linesEdition = new List<string>();

        /// <summary>
        /// Gets or sets the text of the subtitle
        /// </summary>
        public string Text
        {
            get
            {
                return string.Join(Environment.NewLine, lines.ToArray()).Trim();
            }
            set
            {
                lines.Clear();
                lines.AddRange(value.Split(new string[] { "\r\n" }, StringSplitOptions.None));
            }
        }

        public Subtitle(int seqNum)
        {
            sequenceNumber = seqNum;
        }
        public void ParseTime(string line)
        {
            int markerindex = line.IndexOf(" --> ");
            startTime = new SRTTime(line.Substring(0, markerindex));
            endTime = new SRTTime(line.Substring(markerindex + 5, line.Length - markerindex - 5));
        }

        /// <summary>
        /// Multiply the times of this subtitle, for example to convert to other FPS.
        /// </summary>
        public void Multiply(double factor)
        {
            startTime.Multiply(factor);
            endTime.Multiply(factor);
        }

        /// <summary>
        /// Offset the times of this subtitle by a number of milliseconds.
        /// </summary>
        public void Add(int ms)
        {
            startTime.Add(ms);
            endTime.Add(ms);
        }

        /// <summary>
        /// Render the object to .srt format.
        /// </summary>
        /// <returns>the rendered subtitle, with sequence number, times and subtitle itself</returns>
        public string Render()
        {
            return string.Format("{0}\r\n{1} --> {2}\r\n{3}\r\n\r\n",
                sequenceNumber,
                startTime,
                endTime,
                Text);
        }

        public override string ToString()
        {
            return Render();
        }

        internal void Lines_Add(string v)
        {
            if (lines == null)
                lines = new List<string>();

            lines.Add(v);
        }
    }

    public class SRTTime
    {
        private static int
            msInSecond = 1000,
            msInMinute = 60 * msInSecond,
            msInHour = 60 * msInMinute;

        public int Hours, Minutes, Seconds, Milliseconds;

        /// <summary>
        /// Creates a new SRTTime object from a string.
        /// </summary>
        /// <param name="strTime">string in hh:mm:ss,milli format</param>
        public SRTTime(string strTime)
        {
            Parse(strTime);
        }

        /// <summary>
        /// Creates a new SRTTime object from a number of milliseconds.
        /// </summary>
        public SRTTime(int milliseconds)
        {
            TotalMilliseconds = milliseconds;
        }

        /// <summary>
        /// Multiplies the time using 0:0:0,0 as origin.
        /// </summary>
        public void Multiply(double factor)
        {
            TotalMilliseconds = (int)(TotalMilliseconds * factor);
        }

        /// <summary>
        /// Offsets the time my a number of milliseconds.
        /// </summary>
        public void Add(int milliseconds)
        {
            TotalMilliseconds = TotalMilliseconds + milliseconds;
        }

        /// <summary>
        /// Parses a string in hh:mm:ss,milli format
        /// </summary>
        public void Parse(string strTime)
        {
            strTime = strTime.Trim();
            int index1 = strTime.IndexOf(':');
            int index2 = strTime.IndexOf(':', index1 + 1);
            int index3 = strTime.IndexOf(',', index2 + 1);

            //var result = new SRTTime()
            //{
            Hours = int.Parse(strTime.Substring(0, index1));
            Minutes = int.Parse(strTime.Substring(index1 + 1, index2 - index1 - 1));
            Seconds = int.Parse(strTime.Substring(index2 + 1, index3 - index2 - 1));
            Milliseconds = int.Parse(strTime.Substring(index3 + 1, strTime.Length - index3 - 1));
            //};

            //return result;
        }

        /// <summary>
        /// Gets or sets the total amount of milliseconds counted from 0:0:0,0
        /// </summary>
        public int TotalMilliseconds
        {
            get
            {
                return Milliseconds +
                Seconds * msInSecond +
                Minutes * msInMinute +
                Hours * msInHour;
            }
            set
            {
                Hours = (value - value % msInHour) / msInHour;
                value -= Hours * msInHour;

                Minutes = (value - value % msInMinute) / msInMinute;
                value -= Minutes * msInMinute;

                Seconds = (value - value % msInSecond) / msInSecond;
                value -= Seconds * msInSecond;

                Milliseconds = value;
            }
        }

        /// <summary>
        /// Renders SRTTime object to a string hh:mm:ss,milli
        /// </summary>
        /// <returns></returns>
        public string RenderToString()
        {
            return string.Format("{0:D2}:{1:D2}:{2:D2},{3:D3}", Hours, Minutes, Seconds, Milliseconds);
        }

        public override string ToString()
        {
            return RenderToString();
        }

        public static implicit operator TimeSpan(SRTTime time)
        {
            return new TimeSpan(0, time.Hours, time.Minutes, time.Seconds, time.Milliseconds);
        }
        public static implicit operator SRTTime(TimeSpan time)
        {
            return new SRTTime((int)time.TotalMilliseconds);
        }
    }
}
