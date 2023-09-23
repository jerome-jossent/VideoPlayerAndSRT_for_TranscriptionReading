using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
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

namespace VideoPlayerAndSRT_for_TranscriptionReading
{
    /// <summary>
    /// Logique d'interaction pour Subs_UC.xaml
    /// </summary>
    public partial class Subs_UC : UserControl
    {
        private Subtitle sub;
        private MainWindow mainWindow;
        internal bool _isActivated;

        static List<Subs_UC> _subs_activated = new List<Subs_UC>();

        public Subs_UC()
        {
            InitializeComponent();
        }

        public void _Link(MainWindow mainWindow, Subtitle sub)
        {
            this.sub = sub;
            this.mainWindow = mainWindow;

            _tbk_tps_start.Text = sub.StartTime.ToString();
            _tbk.Text = string.Join("\n", sub.Lines);
            _tbk_tps_end.Text = sub.EndTime.ToString();
        }

        public void _SetActive()
        {
            _isActivated = true; // utile à cause du slider qui jump dans la vidéo
            _tbk.FontWeight = FontWeights.Bold;
            _subs_activated.Add(this);
        }
        public void _Deactive()
        {
            _isActivated = false; // utile à cause du slider qui jump dans la vidéo
            _tbk.FontWeight = FontWeights.Normal;
        }

        private void Grid_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left && e.ClickCount == 2)
            {
                mainWindow._SubsGoTo(sub);
            }
        }

        internal static void _InactiveAll()
        {
            foreach (Subs_UC sub in _subs_activated)
                sub._Deactive();

            _subs_activated.Clear();
        }
    }
}
