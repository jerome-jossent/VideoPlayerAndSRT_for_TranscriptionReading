using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Linq;
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

namespace VideoPlayerAndSRT_for_TranscriptionReading
{
    public partial class Subs_UC : UserControl, INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

        private Subtitle sub;
        private MainWindow mainWindow;
        internal bool _isActivated;
        internal bool _isEdited
        {
            get => isEdited;
            set
            {
                isEdited = value;
                UpdateEdit();
            }
        }
        bool isEdited;

        public string _sub_txt
        {
            get => sub.Text; set
            {
                if (sub.Text == value) return;
                sub.Text = value;
                OnPropertyChanged();
            }
        }


        static List<Subs_UC> _subs_activated = new List<Subs_UC>();


        public Subs_UC()
        {
            InitializeComponent();
            DataContext = this;
        }

        public void _Link(MainWindow mainWindow, Subtitle sub)
        {
            this.sub = sub;
            this.mainWindow = mainWindow;

            _tbk_tps_start.Text = sub.startTime.ToString();
            //_tbk.Text = string.Join("\n", sub.lines);
            //_tbx.Text = string.Join("\n", sub.lines);
            _tbk_tps_end.Text = sub.endTime.ToString();

            _isEdited = false;
        }

        public void _SetActive()
        {
            _isActivated = true; // utile à cause du slider qui jump dans la vidéo
            _tbk.FontWeight = FontWeights.Bold;
            _tbx.FontWeight = FontWeights.Bold;
            _subs_activated.Add(this);
        }
        public void _Deactive()
        {
            _isActivated = false; // utile à cause du slider qui jump dans la vidéo
            _tbk.FontWeight = FontWeights.Normal;
            _tbx.FontWeight = FontWeights.Normal;
        }

        //*2 click (gauche) => saut vidéo à ce texte
        void Grid_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left && e.ClickCount == 2)
                mainWindow._SubsGoTo(sub);
        }

        //click droit => édition
        void _tbk_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)            
                mainWindow._Edit(this);            
        }

        //valide l'édition
        void Check_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)            
                mainWindow._Valid(this);            
            else
            {
                //remettre texte d'origine

            }
            _isEdited = false;
        }

        internal static void _InactiveAll()
        {
            foreach (Subs_UC sub in _subs_activated)
                sub._Deactive();

            _subs_activated.Clear();
        }

        void UpdateEdit()
        {
            if (isEdited)
            {
                _tbk.Visibility = Visibility.Hidden;
                grd_Editor.Visibility = Visibility.Visible;
                _tbx.Focus();
                _tbx.SelectionStart = 5;
            }
            else
            {
                grd_Editor.Visibility = Visibility.Hidden;
                _tbk.Visibility = Visibility.Visible;
            }
        }
    }
}
