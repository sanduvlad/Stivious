using System;
using System.Collections.Generic;
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
using Stivious.classes;
using System.IO;
using System.Windows.Forms;
using NAudio;
using NAudio.Wave;
using System.Windows.Media.Animation;
using System.Threading;

namespace Stivious
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private List<Song> Songs = new List<Song>(); //Playlist

        private FolderBrowserDialog folderDialog = new FolderBrowserDialog();
        private OpenFileDialog fileDialog = new OpenFileDialog();

        private int currentSongIndex = 0;


        private bool isShuffle = false;
        private bool isRepeat = false;

        private Color inactiveColor = Color.FromRgb(146, 170, 179);
        private Color activeColor = Color.FromRgb(69, 206, 254);

        AudioFileReader naudio;
        WaveOut waveOut = new WaveOut();

        #region FFT_Spectrum
        FFTSampleProvider sampProv;
        private double logScale = 4;
        private double freqPerPoint = 22000.0 / 512.0;
        private double TRESH = 0.2;
        #endregion

        #region Waveform_Spectrum
        private int waveFormPoints = 1000;
        private double[] waveFormData = new double[1000];
        private int count = 0;
        #endregion

        public MainWindow()
        {
            InitializeComponent();

            ShowPlayButton();

            fileDialog.Multiselect = true;
            fileDialog.Filter = "Music files (*.mp3, *.m4a, *.wma, *.wav) | *.mp3; *.m4a; *.wma; *.wav";

            Shuffle_OFF();
            Repeat_OFF();

            WindowStartupLocation = WindowStartupLocation.CenterScreen;

            LoadPlaylist();

            OnWindowActive(null, null);

            DisplayFrequencies();
        }

        private void DrawHeart()
        {
            int points = 300;
            for (int i = 0; i < points; i = i + 3)
            {


                //polilineFFT.Points.Add(new Point(20 + i * (this.ActualWidth-40) / 1024, 200 - (nValue * 50)));
                var angle = ((360.0 / points) * i) * Math.PI / 180.0;
                var x = 16.0 * Math.Pow(Math.Sin(angle), 3);
                var y = 13.0 * Math.Cos(angle) - 5 * Math.Cos(2 * angle) - 2 * Math.Cos(3 * angle) - Math.Cos(4 * angle);

                polilineHeart.Points.Add(new Point(Width / 2 - x * 10, Height / 2 - 20 - y * 10));
            }
        }

        private void OnWindowInactive(object sender, System.Windows.Input.MouseEventArgs e)
        {
            Console.WriteLine("Window is inactive");

            DoubleAnimation da1 = new DoubleAnimation();
            da1.From = MainPlayerGrid.Opacity;
            da1.To = 0;
            da1.Duration = new Duration(TimeSpan.FromSeconds(3));

            MainPlayerGrid.BeginAnimation(OpacityProperty, da1);

            DoubleAnimation da2 = new DoubleAnimation();
            da2.From = InactivePlayerGrid.Opacity;
            da2.To = 1;
            da2.Duration = new Duration(TimeSpan.FromSeconds(3));

            InactivePlayerGrid.BeginAnimation(OpacityProperty, da2);
        }

        private void OnWindowActive(object sender, System.Windows.Input.MouseEventArgs e)
        {
            Console.WriteLine("Window is active");

            DoubleAnimation da1 = new DoubleAnimation();
            da1.From = InactivePlayerGrid.Opacity;
            da1.To = 0;
            da1.Duration = new Duration(TimeSpan.FromSeconds(0.2));

            InactivePlayerGrid.BeginAnimation(OpacityProperty, da1);

            DoubleAnimation da2 = new DoubleAnimation();
            da2.From = MainPlayerGrid.Opacity;
            da2.To = 1;
            da2.Duration = new Duration(TimeSpan.FromSeconds(0.2));

            MainPlayerGrid.BeginAnimation(OpacityProperty, da2);
        }

        private void WaveOut_PlaybackStopped(object sender, StoppedEventArgs e)
        {
            if (Songs.Count > 0)
            {
                if (isRepeat)
                {
                    PlaySongAtIndex(currentSongIndex);
                }
                else if (isShuffle && Songs.Count > 1)
                {
                    PlaySongAtIndex(new Random().Next(0, Songs.Count - 1));
                }
                else
                {
                    NextSong(null, null);
                }
            }
        }

        private void Switch_Repeat(object sender, MouseButtonEventArgs e)
        {
            if (isRepeat == false)
            {
                isRepeat = true;
                Repeat_ON();
            }
            else
            {
                isRepeat = false;
                Repeat_OFF();
            }
        }

        private void Switch_Shuffle(object sender, MouseButtonEventArgs e)
        {
            if (isShuffle == false)
            {
                isShuffle = true;
                Shuffle_ON();
            }
            else
            {
                isShuffle = false;
                Shuffle_OFF();
            }
        }

        private void Repeat_OFF()
        {
            repeatIcon.Fill = new SolidColorBrush(inactiveColor);
        }

        private void Repeat_ON()
        {
            repeatIcon.Fill = new SolidColorBrush(activeColor);
        }

        private void Shuffle_ON()
        {
            shuffleIcon.Fill = new SolidColorBrush(activeColor);
        }

        private void Shuffle_OFF()
        {
            shuffleIcon.Fill = new SolidColorBrush(inactiveColor);
        }

        private void ShowPlayButton()
        {
            playIcon.Visibility = Visibility.Visible;

            pauseIcon1.Visibility = Visibility.Hidden;
            pauseIcon2.Visibility = Visibility.Hidden;
        }

        private void ShowPauseButton()
        {
            playIcon.Visibility = Visibility.Hidden;

            pauseIcon1.Visibility = Visibility.Visible;
            pauseIcon2.Visibility = Visibility.Visible;
        }


        public void RefreshList()
        {
            if (Songs.Count > 0)
            {
                int index = playListBox.SelectedIndex;

                playListBox.ItemsSource = new List<Song>();

                Songs.Sort();
                playListBox.ItemsSource = Songs;

                if (index >= 0)
                {
                    playListBox.SelectedIndex = index;
                }
            }
            else
            {
                playListBox.ItemsSource = new List<Song>();
                currentSongIndex = -1;
            }
        }

        private void ClearPlaylist(object sender, MouseButtonEventArgs e)
        {
            Songs.Clear();
            RefreshList();
        }

        private void OpenFolder(object sender, MouseButtonEventArgs e)
        {
            if (folderDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                Songs.Clear();
                List<string> files = new List<string>();
                files.AddRange(Directory.GetFiles(folderDialog.SelectedPath, "*.mp3", SearchOption.AllDirectories));
                files.AddRange(Directory.GetFiles(folderDialog.SelectedPath, "*.m4a", SearchOption.AllDirectories));
                files.AddRange(Directory.GetFiles(folderDialog.SelectedPath, "*.wma", SearchOption.AllDirectories));
                files.AddRange(Directory.GetFiles(folderDialog.SelectedPath, "*.wav", SearchOption.AllDirectories));
                for (int i = 0; i < files.Count(); i++)
                {
                    try
                    {
                        Songs.Add(new Song() { name = System.IO.Path.GetFileNameWithoutExtension(files[i]), path = files[i] }); //duration = (new AudioFileReader(files[i]).TotalTime) });
                    }
                    catch { }
                }
                RefreshList();

                currentSongIndex = 0;
            }
        }

        private void OpenSong(object sender, MouseButtonEventArgs e)
        {
            if (fileDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                Songs.Clear();
                List<string> files = new List<string>();
                files.AddRange(fileDialog.FileNames);
                for (int i = 0; i < files.Count; i++)
                {
                    Songs.Add(
                        new Song()
                        {
                            name = System.IO.Path.GetFileNameWithoutExtension(files[i]),
                            path = files[i],
                            duration = (new AudioFileReader(files[i])).TotalTime
                        }
                        );
                }
                RefreshList();
                currentSongIndex = 0;
            }
        }

        private void AddFolder(object sender, MouseButtonEventArgs e)
        {
            if (folderDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                List<string> files = new List<string>();
                files.AddRange(Directory.GetFiles(folderDialog.SelectedPath, "*.mp3", SearchOption.AllDirectories));
                files.AddRange(Directory.GetFiles(folderDialog.SelectedPath, "*.m4a", SearchOption.AllDirectories));
                files.AddRange(Directory.GetFiles(folderDialog.SelectedPath, "*.wma", SearchOption.AllDirectories));
                files.AddRange(Directory.GetFiles(folderDialog.SelectedPath, "*.wav", SearchOption.AllDirectories));
                for (int i = 0; i < files.Count(); i++)
                {
                    if (Songs.Exists(s => s.path == files[i]) == false)
                    {
                        Songs.Add(new Song() { name = System.IO.Path.GetFileNameWithoutExtension(files[i]), path = files[i] }); //duration = (new AudioFileReader(files[i]).TotalTime) });
                    }
                }
                RefreshList();
            }


        }

        private void AddSong(object sender, MouseButtonEventArgs e)
        {
            if (fileDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                List<string> files = new List<string>();
                files.AddRange(fileDialog.FileNames);
                if (files.Count == 0)
                {
                    files.Add(fileDialog.FileName);
                }

                for (int i = 0; i < files.Count; i++)
                {
                    if (Songs.Exists(s => s.path == files[i]) == false)
                    {
                        Songs.Add(
                        new Song()
                        {
                            name = System.IO.Path.GetFileNameWithoutExtension(files[i]),
                            path = files[i],
                            //duration = (new AudioFileReader(files[i])).TotalTime
                        }
                        );
                    }
                }
                RefreshList();
            }


        }

        private void PlaySongAtIndex(int index)
        {

            Console.WriteLine("playing " + Songs[index]);

            waveOut.Stop();
            waveOut.Dispose();
            try
            {
                currentSongIndex = index;
                naudio = new AudioFileReader(Songs[index].path);
                //currentSongIndex = index;
            }
            catch
            {
                naudio = null;
                NextSong(null, null);
                return;
            }

            sampProv = new FFTSampleProvider(naudio);


            waveOut = new WaveOut();

            sampProv.FFT_Calculated += SampProv_FFT_Calculated;
            sampProv.FFT_WaveForm += SampProv_FFT_WaveForm;
            //sampProv.FFT_Volume += SampProv_FFT_Volume;

            waveOut.PlaybackStopped += WaveOut_PlaybackStopped;




            waveOut.Init(sampProv);
            waveOut.Play();

            songLabel.Content = Songs[index].name;



            currentSongIndex = index;
            ShowPauseButton();
        }

        private void SampProv_FFT_WaveForm(object sender, FFT_WaveForm e)
        {

            waveFormData[count] = e.Value[0];
            count++;
            if (count == waveFormPoints)
            {
                waveformData.Points.Clear();
                count = 0;

                //display waveSpectrum
                for (int i = 0; i < waveFormData.Length; i++)
                {
                    if (waveFormData[i] == double.PositiveInfinity || waveFormData[i] > 1)
                    {
                        waveFormData[i] = 1;
                    }
                    if (waveFormData[i] == double.NegativeInfinity || waveFormData[i] < -1)
                    {
                        waveFormData[i] = -1;
                    }

                    var x = Math.Cos(DegreesToRadians(i * (360.0 / waveFormPoints))) * 50;

                    var y = Math.Sin(DegreesToRadians(i * (360.0 / waveFormPoints))) * 50;

                    //Get the length of it, L, and multiply all components by M/L, where M is the new length of the vector. 
                    //x = x * 10+ Width / 2;
                    //y = y * 10 + Height / 2;
                    x = x + x * Math.Abs(waveFormData[i]) + Width / 2;
                    y = y + y * Math.Abs(waveFormData[i]) + Height / 2;

                    //waveformData.Points.Add(new Point(Math.Cos(DegreesToRadians(i * waveFormPoints / 360)) * 50 *  Math.Abs(waveFormData[i]*10) + Width / 2, Height / 2 + Math.Abs(waveFormData[i] * 10) * 50 * Math.Sin(DegreesToRadians(i * waveFormPoints / 360))));
                    waveformData.Points.Add(new Point(x, y));
                }

                waveformData.Points.Add(new Point(waveformData.Points[0].X, waveformData.Points[0].Y));


                waveFormData = new double[1000];
                waveformData.InvalidateVisual();
            }


        }

        private double VectorLength(double x, double y)
        {
            return Math.Sqrt(x * x + y * y);
        }

        private int GetSign(double deg)
        {
            if (deg == 0)
            {
                return 1;
            }
            return (int)(deg / Math.Abs(deg));
        }

        private double DegreesToRadians(double deg)
        {
            return deg * Math.PI / 180;
        }

        private void DisplayFrequencies()
        {
            InactivePlayerGrid.Children.Add(GetLabelInstance("Hz", new Thickness(20, (Height - 25), 0, 0)));
            InactivePlayerGrid.Children.Add(GetLabelInstance("70", new Thickness(20 + (Math.Sqrt(70)) * logScale - 15, (Height - 25), 0, 0)));
            InactivePlayerGrid.Children.Add(GetLabelInstance("200", new Thickness(20 + (Math.Sqrt(200)) * logScale - 15, (Height - 25), 0, 0)));
            InactivePlayerGrid.Children.Add(GetLabelInstance("500", new Thickness(20 + (Math.Sqrt(500)) * logScale - 15, (Height - 25), 0, 0)));
            InactivePlayerGrid.Children.Add(GetLabelInstance("1k", new Thickness(20 + (Math.Sqrt(1000)) * logScale - 15, (Height - 25), 0, 0)));
            InactivePlayerGrid.Children.Add(GetLabelInstance("2k", new Thickness(20 + (Math.Sqrt(2000)) * logScale - 15, (Height - 25), 0, 0)));
            InactivePlayerGrid.Children.Add(GetLabelInstance("3k", new Thickness(20 + (Math.Sqrt(3000)) * logScale - 15, (Height - 25), 0, 0)));
            InactivePlayerGrid.Children.Add(GetLabelInstance("5k", new Thickness(20 + (Math.Sqrt(5000)) * logScale - 15, (Height - 25), 0, 0)));
            InactivePlayerGrid.Children.Add(GetLabelInstance("8k", new Thickness(20 + (Math.Sqrt(8000)) * logScale - 15, (Height - 25), 0, 0)));
            InactivePlayerGrid.Children.Add(GetLabelInstance("10k", new Thickness(20 + (Math.Sqrt(10000)) * logScale - 15, (Height - 25), 0, 0)));
            InactivePlayerGrid.Children.Add(GetLabelInstance("13k", new Thickness(20 + (Math.Sqrt(13000)) * logScale - 15, (Height - 25), 0, 0)));
            InactivePlayerGrid.Children.Add(GetLabelInstance("15k", new Thickness(20 + (Math.Sqrt(15000)) * logScale - 15, (Height - 25), 0, 0)));
        }

        private System.Windows.Controls.Label GetLabelInstance(string text, Thickness margin)
        {
            System.Windows.Controls.Label label = new System.Windows.Controls.Label();
            label.FontSize = 10;
            label.Foreground = new SolidColorBrush(Colors.White);
            label.Content = text;

            label.Margin = margin;

            return label;
        }

        private void SampProv_FFT_Calculated(object sender, FFT_EventArgs e)
        {
            polilineFFT.Points.Clear();

            for (int i = 0; i < e.Result.Length / 2; i++)
            {
                var value = (10 * Math.Log(Math.Sqrt(e.Result[i].X * e.Result[i].X + e.Result[i].Y * e.Result[i].Y)));
                var nValue = Normalize(value);

                if (nValue == double.NegativeInfinity || nValue <= 0)
                {
                    nValue = 0;
                }
                if (nValue == double.PositiveInfinity)
                {
                    nValue = 1;
                }
                nValue = 150 * nValue;
                if (nValue < 0) nValue = 0;

                if (i == 0)
                {
                    polilineFFT.Points.Add(new Point(20, Height - 20));
                }
                else
                {
                    if (i * freqPerPoint <= 15000)
                    {
                        var x = polilineFFT.Points[0].X + (Math.Sqrt(i * freqPerPoint)) * logScale;
                        var y = (Height - 20) - nValue;
                        polilineFFT.Points.Add(new Point(x, y));


                        if (i > 2 && polilineFFT.Points[i - 1].Y / 200 < TRESH)
                        {
                            polilineFFT.Points[i - 1] = new Point(polilineFFT.Points[i - 1].X,
                                (polilineFFT.Points[i - 2].Y + polilineFFT.Points[i].Y) / 2
                                );
                        }
                    }
                }
            }
            polilineFFT.Points.Add(new Point(Width - 20, (Height - 20)));
            this.InvalidateVisual();
        }





        private double GetDistance(double x, double y)
        {
            return Math.Sqrt(x * x + y * y);
        }

        private double Normalize(double value)
        {
            return (1 - 0.0) / (0 + 90.0) * (value - 0.0) + 1.0;
        }

        private void ResumeSong()
        {
            if (waveOut.PlaybackState == PlaybackState.Paused)
            {
                waveOut.Resume();
            }
        }

        private void PauseSong()
        {
            if (waveOut.PlaybackState == PlaybackState.Playing)
            {
                waveOut.Pause();
            }
        }

        private void playListBox_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            Console.WriteLine("double clicked on a song in playlist !");

            if (Songs.Count != 0)
            {
                PlaySongAtIndex(playListBox.SelectedIndex);
            }
        }

        private void PlayPauseButton(object sender, MouseButtonEventArgs e)
        {
            Console.WriteLine("play button clicked !");

            if (Songs.Count > 0)
            {
                if (waveOut.PlaybackState == PlaybackState.Paused)
                {
                    ResumeSong();
                    ShowPauseButton();
                }
                else if (waveOut.PlaybackState == PlaybackState.Playing)
                {
                    PauseSong();
                    ShowPlayButton();
                }
                else if (waveOut.PlaybackState == PlaybackState.Stopped)
                {
                    PlaySongAtIndex(currentSongIndex);
                    ShowPauseButton();
                }
            }
        }

        private void NextSong(object sender, MouseButtonEventArgs e)
        {
            Console.WriteLine("next button pressed !");

            if (currentSongIndex + 1 < Songs.Count)
            {
                currentSongIndex++;

                PlaySongAtIndex(currentSongIndex);

                ShowPauseButton();
            }
        }

        private void PreviousSong(object sender, MouseButtonEventArgs e)
        {
            Console.WriteLine("previous button pressed !");

            if (currentSongIndex - 1 >= 0)
            {
                currentSongIndex--;

                PlaySongAtIndex(currentSongIndex);

                ShowPauseButton();
            }
        }

        private void CloseAplication(object sender, MouseButtonEventArgs e)
        {
            Console.WriteLine("application will close !");

            using (StreamWriter sw = new StreamWriter("resources/list.txt"))
            {
                for (int i = 0; i < Songs.Count; i++)
                {
                    sw.WriteLine(Songs[i].path);
                }
            }
            System.Windows.Application.Current.Shutdown();
        }

        private void LoadPlaylist()
        {
            if (File.Exists("resources/list.txt"))
            {
                using (StreamReader sr = new StreamReader("resources/list.txt"))
                {
                    string line = string.Empty;
                    while (string.IsNullOrEmpty((line = sr.ReadLine())) == false)
                    {
                        if (string.IsNullOrEmpty(line) == false && File.Exists(line) == true)
                        {
                            Songs.Add(new Song() { name = System.IO.Path.GetFileNameWithoutExtension(line), path = line }); //duration = (new AudioFileReader(line).TotalTime) });
                        }
                    }
                }
                //File.Delete("resources/list.txt");

                RefreshList();
            }

            //if (File.Exists("resources/output.txt"))
            //{
            //    using (StreamReader sr = new StreamReader("resources/output.txt"))
            //    {
            //        string line = string.Empty;
            //        while (string.IsNullOrEmpty((line = sr.ReadLine())) == false)
            //        {
            //            if (string.IsNullOrEmpty(line) == false)
            //            {
            //                Songs.Add(new Song() { name = line.Split('/')[line.Split('/').Length - 1].Replace("%20", " ").Replace("&amp;", "&"), path = line }); //duration = (new AudioFileReader(line).TotalTime) });
            //            }
            //        }
            //    }

            //    RefreshList();
            //}
        }

        private void MinimizeApplication(object sender, MouseButtonEventArgs e)
        {
            WindowState = WindowState.Minimized;
        }

        private void DragWindow(object sender, MouseButtonEventArgs e)
        {
            Console.WriteLine("window is being draged !");

            if (e.ChangedButton == MouseButton.Left)
                this.DragMove();
        }

        private void slider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            //waveOut.Volume = (float)slider.Value;
        }

        private void button_Click(object sender, RoutedEventArgs e)
        {
            //waveOut.Stop();
            //waveOut.Dispose();
            //try
            //{
            //    currentSongIndex = index;
            //    naudio = new AudioFileReader(Songs[index].path);
            //    //currentSongIndex = index;
            //}
            //catch
            //{
            //    naudio = null;
            //    NextSong(null, null);
            //    return;
            //}

            //sampProv = new FFTSampleProvider(naudio);


            //waveOut = new WaveOut();

            //sampProv.FFT_Calculated += SampProv_FFT_Calculated;
            //sampProv.FFT_WaveForm += SampProv_FFT_WaveForm;

            //waveOut.PlaybackStopped += WaveOut_PlaybackStopped;


            //waveOut.Init(sampProv);
            //waveOut.Play();

            //songLabel.Content = Songs[index].name;
        }

        private bool isActiveSearch = false;
        private void Grid_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            Console.WriteLine("Entered Grid_keydown");
            if (e.Key != Key.Escape && e.Key != Key.Space && isActiveSearch == false)
            {
                isActiveSearch = true;
                SearchGrid.Visibility = Visibility.Visible;
                System.Windows.Controls.Panel.SetZIndex(SearchGrid, 3);

                //System.Windows.Controls.Panel.SetZIndex(searchBox, 102);
                //System.Windows.Controls.Panel.SetZIndex(searchResults, 102);
                searchBox.Focus();
                searchBox.Text = string.Empty;

            }
            if (e.Key == Key.Escape)
            {
                isActiveSearch = false;
                SearchGrid.Visibility = Visibility.Hidden;

                //System.Windows.Controls.Panel.SetZIndex(SearchGrid, 0);
                //System.Windows.Controls.Panel.SetZIndex(searchBox, 98);
                //System.Windows.Controls.Panel.SetZIndex(searchResults, 98);
            }
        }

        private void CloseSearchGrid(object sender, MouseButtonEventArgs e)
        {
            isActiveSearch = false;
            SearchGrid.Visibility = Visibility.Hidden;
        }

        private void searchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            short contains = 0;
            List<object> results = new List<object>();
            searchResults.ItemsSource = results;
            for (int i = 0; i < Songs.Count; i++)
            {
                contains = 0;
                string[] words = searchBox.Text.Split(' ');
                for (int j = 0; j < words.Length; j++)
                {
                    if (words[j] != string.Empty && Songs[i].name.ToLower().Contains(words[j].ToLower()))
                    {
                        contains++;
                        break;
                    }
                }
                if (contains == words.Count())
                {
                    results.Add(new { Name = Songs[i].name, Index = i });
                }
            }

            searchResults.ItemsSource = results;
        }

        private void searchResults_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (searchResults.Items.Count > 0 && searchResults.SelectedIndex != -1)
            {
                PlaySongAtIndex(((dynamic)searchResults.Items[searchResults.SelectedIndex]).Index);
            }
        }
    }
}