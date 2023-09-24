using NAudio_JJ;
using PanAndZoom;
using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Threading;


namespace AudioVolumeAmplitudeGraph
{
    public partial class Graph : UserControl
    {
        //VolumeAnalysis volumeAnalysis;


        Dictionary<Silence, Pastille> silences_pastille_V;
        Dictionary<ListBoxItem, Silence> listitems_silence;
        Pastille previousPastilleSelected;
        Polygon sound_peaks_V;
        Polygon sound_silences_V;
        Polygon silence_selected;
        Polyline play_cursor_playing_V;
        System.Windows.Media.Color play_cursor_playing_color = System.Windows.Media.Colors.White;

        double current_time, current_playing_time;

        public enum ZLevelOnCanvas { tracks = 0, peaks = 3, silences = 5, pastilles = 1000, cursor = 2000 }





        TranslateTransform st_V;
        ScaleTransform sc_V;

        public Graph()
        {
            InitializeComponent();
        }

        void Silence_selected_delete(object sender, RoutedEventArgs e)
        {
            //data.silences.Remove((Silence)lbox_silence.SelectedItem);
            List_Silences();
            ReDrawGraph();
        }



        void List_Titles()
        {
            for (int i = 0; i < volumeAnalysis.data.titles.Count; i++)
            {
                Title title = volumeAnalysis.data.titles[i];
                Title_UC uc = new Title_UC();
                uc._Link(title);
            }

            //lbox.Items.Clear();
            //for (int i = 0; i < data.titles.Count; i++)
            //    lbox.Items.Add(data.titles[i].uc);
        }

        void List_Silences()
        {
            //lbox_silence.Items.Clear();
            listitems_silence = new Dictionary<ListBoxItem, Silence>();

            ListBoxItem it = null;
            for (int i = 0; i < volumeAnalysis.data.silences.Count; i++)
            {
                volumeAnalysis.data.silences[i].index = i + 1;
                it = new ListBoxItem();
                it.Content = volumeAnalysis.data.silences[i];
                it.MouseEnter += new System.Windows.Input.MouseEventHandler(SilenceOver);
                //lbox_silence.Items.Add(it);
                listitems_silence.Add(it, volumeAnalysis.data.silences[i]);
            }
        }
        private void SilenceOver(object? sender, System.Windows.Input.MouseEventArgs e)
        {
            previousPastilleSelected?._FocusLost();
            ListBoxItem it = (ListBoxItem)sender;

            if (listitems_silence.ContainsKey(it))
                if (silences_pastille_V.ContainsKey(listitems_silence[it]))
                {
                    previousPastilleSelected = silences_pastille_V[listitems_silence[it]];
                    previousPastilleSelected._Focus();
                }
        }


        void PreProcess()
        {
            List_Silences();
            //Read text => make a list of titles
            //TitlesMaker(txt.Text, data.titles, author.Text, album.Text);
            List_Titles();

            //représentations graphiques
            ReDrawGraph();
            //_ready_to_Process = true;
        }


        void ReDrawGraph()
        {
            for (int i = 0; i < rectangles_V.Children.Count; i++)
            {
                var item = rectangles_V.Children[i];
                if (item is Pastille)
                    continue;

                if (item == play_cursor_playing_V)
                    continue;

                rectangles_V.Children.RemoveAt(i);
                i--;
            }
            Draw_Titles_V();
            Draw_Peaks_V(System.Windows.Media.Color.FromRgb(40, 40, 40));
            Draw_Silences_V(Colors.White);
            DrawOrUpdate_SilencesPastilles_V();
        }

        void Draw_Titles_V()
        {
            if (volumeAnalysis.data.titles == null)
                return;

            foreach (Title titre in volumeAnalysis.data.titles)
            {
                // Create the rectangle
                System.Windows.Shapes.Rectangle rec = new System.Windows.Shapes.Rectangle()
                {
                    //Width = titre.end.TotalSeconds - titre.start.TotalSeconds,// rectangles.Width,
                    Width = 1,// rectangles.Width,
                    Height = titre.end.TotalSeconds - titre.start.TotalSeconds,
                    //Height = 1,
                    Fill = titre.brush,
                    Stroke = System.Windows.Media.Brushes.Black,
                    StrokeThickness = 0,
                };
                titre.rectangle = rec;

                //Add to canvas
                Dispatcher.Invoke(new Action(() =>
                {
                    rectangles_V.Children.Add(rec);
                    System.Windows.Controls.Panel.SetZIndex(rec, (int)ZLevelOnCanvas.tracks);
                    Canvas.SetTop(rec, titre.start.TotalSeconds);
                    Canvas.SetLeft(rec, 0);
                }));
            }
        }
        void Draw_Peaks_V(System.Windows.Media.Color color)
        {
            //dessine le niveau sonore = f(temps)
            sound_peaks_V = new Polygon();
            sound_peaks_V.Fill = new SolidColorBrush(color);
            sound_peaks_V.Points.Add(new System.Windows.Point(0, 0));

            for (int i = 0; i < volumeAnalysis.data.peaks.Count; i++)
                sound_peaks_V.Points.Add(new System.Windows.Point(volumeAnalysis.data.peaks[i].amplitude, volumeAnalysis.data.peaks[i].temps));

            sound_peaks_V.Points.Add(new System.Windows.Point(0, volumeAnalysis.data.totaltime));
            sound_peaks_V.Points.Add(new System.Windows.Point(0, 0));

            //positionnement du dessin
            rectangles_V.Children.Add(sound_peaks_V);
            System.Windows.Controls.Panel.SetZIndex(sound_peaks_V, (int)ZLevelOnCanvas.peaks);
            Canvas.SetTop(sound_peaks_V, 0);
            Canvas.SetLeft(sound_peaks_V, 0);

            rectangles_V.Height = volumeAnalysis.data.totaltime;
        }
        void Draw_Silences_V(System.Windows.Media.Color color)
        {
            //dessine des traits à chaque silence = f(temps)
            sound_silences_V = new Polygon();
            sound_silences_V.Fill = new SolidColorBrush(color);
            sound_silences_V.Points.Add(new System.Windows.Point(1, 0));

            double X, Y;
            foreach (Silence silence in volumeAnalysis.data.silences)
            {
                X = 1;
                Y = silence.debut;
                sound_silences_V.Points.Add(new System.Windows.Point(X, Y));
                X = 0.1;
                sound_silences_V.Points.Add(new System.Windows.Point(X, Y));
                Y = (double)silence.fin;
                sound_silences_V.Points.Add(new System.Windows.Point(X, Y));
                X = 1;
                sound_silences_V.Points.Add(new System.Windows.Point(X, Y));
            }
            sound_silences_V.Points.Add(new System.Windows.Point(1, 0));

            //positionnement du dessin
            rectangles_V.Children.Add(sound_silences_V);
            System.Windows.Controls.Panel.SetZIndex(sound_silences_V, (int)ZLevelOnCanvas.silences);
            Canvas.SetTop(sound_silences_V, 0);
            Canvas.SetLeft(sound_silences_V, 0);

            rectangles_V.Height = volumeAnalysis.data.totaltime;
        }





        void ZoomBorder_V_MoveEvent(object sender, PanAndZoom.ZoomBorderEventArgs e)
        {
            if (volumeAnalysis.data == null) return;
            double y_relative = (e.mouseRelativeY - e.relativeoffsetY) / e.scaleY;
            current_time = y_relative * volumeAnalysis.data.totaltime;
            TimeSpan t = TimeSpan.FromSeconds(current_time);
            //string titre = _Title + " - " + t.ToString("G");
            //Dispatcher.BeginInvoke(() => (Title = titre));
        }

        void ZoomBorder_V_ZoomChangeEvent(object sender, ZoomBorderEventArgs args)
        {
            DrawOrUpdate_SilencesPastilles_V();
            UpdateCursorThickness_V();
        }

        void ZoomBorder_V_MouseLeftButtonWithoutMoveEvent(object sender, ZoomBorderEventArgs e)
        {
            if (e == null) return;
            double y_relative = (e.mouseRelativeY - e.relativeoffsetY) / e.scaleY;
            current_playing_time = y_relative * volumeAnalysis.data.totaltime;
            if (NAudio_JJ.NAudio_JJ.isPlaying)
                PlayAudioHere();
            else
                DrawOrUpdate_PlayCursor_V(current_playing_time);
        }

        void PlayAudioHere()
        {
            NAudio_JJ.NAudio_JJ.playerPlayingEvent += NAudio_JJ_playerPlayingEvent; ;
            NAudio_JJ.NAudio_JJ.AudioPlayer_Play(volumeAnalysis.filename, current_playing_time, volumeAnalysis.data.totaltime);
        }
        private void NAudio_JJ_playerPlayingEvent(object sender, NAudio_JJ.NAudio_JJ.PlayerPlayingEventArgs e)
        {
            current_playing_time = e.Val;
            try
            {
                Dispatcher.Invoke(() =>
                {
                    DrawOrUpdate_PlayCursor_V(e.Val);
                });
            }
            catch (Exception ex)
            {
            }
        }

        private void GridZoom_V_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            DrawOrUpdate_SilencesPastilles_V();
            UpdateCursorThickness_V();
        }
        private void UpdateCursorThickness_V()
        {
            if (sc_V != null && play_cursor_playing_V != null)
                play_cursor_playing_V.StrokeThickness = rectangles_V.ActualHeight * 0.2 / (100 * sc_V.ScaleY);
        }

        void DrawOrUpdate_SilencesPastilles_V()
        {
            if (volumeAnalysis.data == null) return;

            if (st_V == null) st_V = zoomBorder_V._GetTranslateTransform();
            if (st_V == null) return;

            if (sc_V == null) sc_V = zoomBorder_V._GetScaleTransform();
            if (sc_V == null) return;

            if (silences_pastille_V == null)
            {
                silences_pastille_V = new Dictionary<Silence, Pastille>();
                foreach (Silence silence in volumeAnalysis.data.silences)
                {
                    Pastille pastille = new Pastille();
                    pastille.Set(silence.index.ToString("00"),
                        stroke_color: System.Windows.Media.Brushes.Black,
                        fill_color: System.Windows.Media.Brushes.White,
                        stroke_thickness: 1,
                        silence,
                        (int)ZLevelOnCanvas.pastilles - silence.index
                        );
                    rectangles_V.Children.Add(pastille);
                    silences_pastille_V.Add(silence, pastille);
                }
            }

            double rectangles_W_abs = rectangles_V.ActualWidth / sc_V.ScaleX;
            double rectangles_H_abs = volumeAnalysis.data.totaltime / sc_V.ScaleY;
            double fixedwidth_prct = 0.03 * zoomBorder_V.ActualHeight / zoomBorder_V.ActualWidth;
            double fixedheight_prct = fixedwidth_prct * zoomBorder_V.ActualWidth / zoomBorder_V.ActualHeight;

            //mis à jour du positionnement des pastilles
            foreach (var item in silences_pastille_V)
            {
                Silence silence = item.Key;
                Pastille pastille = item.Value;

                pastille.Width = rectangles_W_abs * fixedwidth_prct;
                pastille.Height = rectangles_H_abs * fixedheight_prct;

                double top = silence.milieu - pastille.Height / 2;
                double left = rectangles_V.Width - pastille.Width;//à droite

                //met les nouvelles pastilles derrières les anciennes;
                System.Windows.Controls.Panel.SetZIndex(pastille, pastille._zindex);
                Canvas.SetTop(pastille, top);
                Canvas.SetLeft(pastille, left);
            }
        }

        void DrawOrUpdate_PlayCursor_V(double val)
        {
            if (volumeAnalysis.data == null) return;

            if (st_V == null) st_V = zoomBorder_V._GetTranslateTransform();
            if (st_V == null) return;

            if (sc_V == null) sc_V = zoomBorder_V._GetScaleTransform();
            if (sc_V == null) return;

            if (play_cursor_playing_V == null)
            {
                play_cursor_playing_V = new Polyline();
                play_cursor_playing_V.Points.Add(new System.Windows.Point(1, val));
                play_cursor_playing_V.Points.Add(new System.Windows.Point(0, val));
                play_cursor_playing_V.Stroke = new SolidColorBrush(play_cursor_playing_color);
                rectangles_V.Children.Add(play_cursor_playing_V);
                Canvas.SetTop(play_cursor_playing_V, 0);
                Canvas.SetLeft(play_cursor_playing_V, 0);
                System.Windows.Controls.Panel.SetZIndex(play_cursor_playing_V, (int)ZLevelOnCanvas.cursor);
            }
            else
            {
                play_cursor_playing_V.Points[0] = new System.Windows.Point(1, val);
                play_cursor_playing_V.Points[1] = new System.Windows.Point(0, val);
            }
            UpdateCursorThickness_V();
        }

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            zoomBorder_V.MoveEvent += ZoomBorder_V_MoveEvent;
            zoomBorder_V.ZoomChangeEvent += ZoomBorder_V_ZoomChangeEvent;
            zoomBorder_V.MouseLeftButtonWithoutMoveEvent += ZoomBorder_V_MouseLeftButtonWithoutMoveEvent;
        }

    }
}