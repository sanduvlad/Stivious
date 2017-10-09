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
using NAudio.Dsp;
using System.ComponentModel;
using MultipleAnimations;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Security.Permissions;

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

        FFTSampleProvider sampProv;

        Thread SpectrumTrianglesTrhead;

        Random random = new Random();


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


            SpectrumTrianglesTrhead = new Thread(() => GenerateTrianglesAnimationThreadFunction(InactivePlayerGrid, waveOut));
            SpectrumTrianglesTrhead.SetApartmentState(ApartmentState.STA);
            SpectrumTrianglesTrhead.Start();

            DisplayFrequencies();
            LoadBackgroundImage();
        }

        private void SaveImageBackgroundToText(string fileName)
        {
            using (StreamWriter sw = new StreamWriter(@"resources/backgroundImage.txt"))
            {
                sw.WriteLine(fileName);
            }
        }

        private void LoadBackgroundImage()
        {
            if (File.Exists(@"resources/backgroundImage.txt"))
            {
                using (StreamReader sr = new StreamReader(@"resources/backgroundImage.txt"))
                {
                    string fileName = "";
                    if ( File.Exists(fileName = sr.ReadLine()))
                    {
                        this.InactivePlayerGrid.ClearValue(BackgroundProperty);
                        this.InactivePlayerGrid.Background = new ImageBrush();


                        var imageBr = new ImageBrush(new BitmapImage(new Uri(fileName)));
                        imageBr.Stretch = Stretch.UniformToFill;
                        this.InactivePlayerGrid.Background = imageBr;
                    }
                }
            }
        }

        private void GenerateTrianglesAnimationThreadFunction(object grid, object waveout)
        {

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

                //Songs.Sort();
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
                        Songs.Add(new Song() { name = System.IO.Path.GetFileNameWithoutExtension(files[i]), path = files[i] });
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
                            path = files[i]
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
                        Songs.Add(new Song() { name = System.IO.Path.GetFileNameWithoutExtension(files[i]), path = files[i] });
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
                            path = files[i]
                        }
                        );
                    }
                }
                RefreshList();
            }


        }

        private void PlaySongAtIndex(int index)
        {
            if (index == -1) return;

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
            sampProv.FFT_Volume += SampProv_FFT_Volume;

            waveOut.PlaybackStopped += WaveOut_PlaybackStopped;




            waveOut.Init(sampProv);
            waveOut.Play();

            songLabel.Content = Songs[index].name;



            currentSongIndex = index;
            ShowPauseButton();
        }

        private double opacity_TRESH = 0.3;

        private void SampProv_FFT_Volume(object sender, FFT_VolumeMeter e)
        {

        }

        private int waveFormPoints = 1000;
        private double[] waveFormData = new double[1000];
        private int count = 0;

        private void SampProv_FFT_WaveForm(object sender, FFT_WaveForm e)
        {

            waveFormData[count] = e.Value[0];
            count++;
            if (count == waveFormPoints)
            {
                //particle
                //ParticleDelay++;
                //if (ParticleDelay == 30)
                //{
                //    ParticleDelay = 0;

                //        var r = random.Next();
                //        var x = Math.Cos(DegreesToRadians(r)) + Width / 2;
                //        var y = Math.Sin(DegreesToRadians(r)) + Height / 2;

                //        AnimatedPolygon ap = new AnimatedPolygon((int)this.Height, (int)this.Width, Colors.White, InactivePlayerGrid, DateTime.Now.Millisecond, new Point(x, y), r);
                //}
                //

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

        private double logScale = 5.3;

        private double freqPerPoint = 22000.0 / 512.0;

        private double linearSpace = Math.Log(6000) - Math.Log(3000);

        private double TRESH = 0.2;

        private void DisplayFrequencies()
        {
            InactivePlayerGrid.Children.Add(GetLabelInstance("Hz", new Thickness(20, (Height - 25), 0, 0)));
            InactivePlayerGrid.Children.Add(GetLabelInstance("100", new Thickness(10 + Math.Sqrt(100) * logScale, (Height - 25), 0, 0)));
            InactivePlayerGrid.Children.Add(GetLabelInstance("300", new Thickness(10 + Math.Sqrt(300) * logScale, (Height - 25), 0, 0)));
            InactivePlayerGrid.Children.Add(GetLabelInstance("500", new Thickness(10 + Math.Sqrt(500) * logScale, (Height - 25), 0, 0)));
            InactivePlayerGrid.Children.Add(GetLabelInstance("1k", new Thickness(10 + Math.Sqrt(1000) * logScale, (Height - 25), 0, 0)));
            InactivePlayerGrid.Children.Add(GetLabelInstance("2k", new Thickness(10 + Math.Sqrt(2000) * logScale, (Height - 25), 0, 0)));
            InactivePlayerGrid.Children.Add(GetLabelInstance("3k", new Thickness(10 + Math.Sqrt(3000) * logScale, (Height - 25), 0, 0)));
            InactivePlayerGrid.Children.Add(GetLabelInstance("5k", new Thickness(10 + Math.Sqrt(5000) * logScale, (Height - 25), 0, 0)));
            InactivePlayerGrid.Children.Add(GetLabelInstance("10k", new Thickness(10 + Math.Sqrt(10000) * logScale, (Height - 25), 0, 0)));
            InactivePlayerGrid.Children.Add(GetLabelInstance("13k", new Thickness(10 + Math.Sqrt(13000) * logScale, (Height - 25), 0, 0)));
            InactivePlayerGrid.Children.Add(GetLabelInstance("15k", new Thickness(10 + Math.Sqrt(15000) * logScale, (Height - 25), 0, 0)));
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

                if (i == 0)
                {
                    polilineFFT.Points.Add(new Point(20, Height - 20));
                }

                if (i * freqPerPoint <= 15500)
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

            SpectrumTrianglesTrhead.Abort();
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
                            Songs.Add(new Song() { name = System.IO.Path.GetFileNameWithoutExtension(line), path = line });
                        }
                    }
                }
                RefreshList();
            }
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
            //List<object> results = new List<object>();
            BindingList<object> result = new BindingList<object>();
            searchResults.ItemsSource = result;
            for (int i = 0; i < Songs.Count; i++)
            {
                contains = 0;
                string[] words = searchBox.Text.Split(' ');
                for (int j = 0; j < words.Length; j++)
                {
                    if (words[j] != string.Empty && Songs[i].name.ToLower().Contains(words[j].ToLower()))
                    {
                        contains++;
                    }
                }
                if (contains == words.Count() || contains == words.Count(w => w.Length > 0))
                {
                    //searchResults.Items.Add( new { Name = Songs[i].name, Index = i });
                    result.Add(new { Name = Songs[i].name, Index = i });
                }
            }

            //searchResults.ItemsSource = results;
            searchResults.InvalidateArrange();
        }

        private void searchResults_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (searchResults.Items.Count > 0 && searchResults.SelectedIndex != -1)
            {
                PlaySongAtIndex(((dynamic)searchResults.Items[searchResults.SelectedIndex]).Index);
            }
        }

        

        public void ChangeBackgroundImage(object sender, MouseButtonEventArgs e)
        {
            OpenFileDialog fl = new OpenFileDialog();
            fl.Filter = "Image files (*.png, *.jpg) | *.png; *.jpg";
            if (fl.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                this.InactivePlayerGrid.ClearValue(BackgroundProperty);
                this.InactivePlayerGrid.Background = new ImageBrush();

                
                var imageBr = new ImageBrush(new BitmapImage(new Uri(fl.FileName)));
                imageBr.Stretch = Stretch.UniformToFill;
                this.InactivePlayerGrid.Background = imageBr;
            }
            fl.Dispose();

            SaveImageBackgroundToText(fl.FileName);
        }

        #region proceeseseseses
        public List<Process> GetProcessesLockingFile(string filePath)
        {
            var procs = new List<Process>();

            var processListSnapshot = Process.GetProcesses();
            foreach (var process in processListSnapshot)
            {
                Console.WriteLine(process.ProcessName);
                if (process.Id <= 4) { continue; } // system processes
                var files = GetFilesLockedBy(process);
                if (files.Contains(filePath))
                {

                    Console.WriteLine("--------------->" + process.ProcessName);
                    procs.Add(process);
                }
            }
            return procs;
        }

        /// <summary>
        /// Return a list of file locks held by the process.
        /// </summary>
        public List<string> GetFilesLockedBy(Process process)
        {
            var outp = new List<string>();

            ThreadStart ts = delegate
            {
                try
                {
                    outp = UnsafeGetFilesLockedBy(process);
                }
                catch { Ignore(); }
            };

            try
            {
                var t = new Thread(ts);
                t.IsBackground = true;
                t.Start();
                if (!t.Join(250))
                {
                    try
                    {
                        t.Interrupt();
                        t.Abort();
                    }
                    catch { Ignore(); }
                }
            }
            catch { Ignore(); }

            return outp;
        }


        #region Inner Workings
        private void Ignore() { }
        private List<string> UnsafeGetFilesLockedBy(Process process)
        {
            try
            {
                var handles = GetHandles(process);
                var files = new List<string>();

                foreach (var handle in handles)
                {
                    var file = GetFilePath(handle, process);
                    if (file != null) files.Add(file);
                }

                return files;
            }
            catch
            {
                return new List<string>();
            }
        }

        const int CNST_SYSTEM_HANDLE_INFORMATION = 16;
        private string GetFilePath(Win32API.SYSTEM_HANDLE_INFORMATION systemHandleInformation, Process process)
        {
            var ipProcessHwnd = Win32API.OpenProcess(Win32API.ProcessAccessFlags.All, false, process.Id);
            var objBasic = new Win32API.OBJECT_BASIC_INFORMATION();
            var objObjectType = new Win32API.OBJECT_TYPE_INFORMATION();
            var objObjectName = new Win32API.OBJECT_NAME_INFORMATION();
            var strObjectName = "";
            var nLength = 0;
            IntPtr ipTemp, ipHandle;

            if (!Win32API.DuplicateHandle(ipProcessHwnd, systemHandleInformation.Handle, Win32API.GetCurrentProcess(), out ipHandle, 0, false, Win32API.DUPLICATE_SAME_ACCESS))
                return null;

            IntPtr ipBasic = Marshal.AllocHGlobal(Marshal.SizeOf(objBasic));
            Win32API.NtQueryObject(ipHandle, (int)Win32API.ObjectInformationClass.ObjectBasicInformation, ipBasic, Marshal.SizeOf(objBasic), ref nLength);
            objBasic = (Win32API.OBJECT_BASIC_INFORMATION)Marshal.PtrToStructure(ipBasic, objBasic.GetType());
            Marshal.FreeHGlobal(ipBasic);

            IntPtr ipObjectType = Marshal.AllocHGlobal(objBasic.TypeInformationLength);
            nLength = objBasic.TypeInformationLength;
            // this one never locks...
            while ((uint)(Win32API.NtQueryObject(ipHandle, (int)Win32API.ObjectInformationClass.ObjectTypeInformation, ipObjectType, nLength, ref nLength)) == Win32API.STATUS_INFO_LENGTH_MISMATCH)
            {
                if (nLength == 0)
                {
                    Console.WriteLine("nLength returned at zero! ");
                    return null;
                }
                Marshal.FreeHGlobal(ipObjectType);
                ipObjectType = Marshal.AllocHGlobal(nLength);
            }

            objObjectType = (Win32API.OBJECT_TYPE_INFORMATION)Marshal.PtrToStructure(ipObjectType, objObjectType.GetType());
            if (Is64Bits())
            {
                ipTemp = new IntPtr(Convert.ToInt64(objObjectType.Name.Buffer.ToString(), 10) >> 32);
            }
            else
            {
                ipTemp = objObjectType.Name.Buffer;
            }

            var strObjectTypeName = Marshal.PtrToStringUni(ipTemp, objObjectType.Name.Length >> 1);
            Marshal.FreeHGlobal(ipObjectType);
            if (strObjectTypeName != "File")
                return null;

            nLength = objBasic.NameInformationLength;

            var ipObjectName = Marshal.AllocHGlobal(nLength);

            // ...this call sometimes hangs. Is a Windows error.
            while ((uint)(Win32API.NtQueryObject(ipHandle, (int)Win32API.ObjectInformationClass.ObjectNameInformation, ipObjectName, nLength, ref nLength)) == Win32API.STATUS_INFO_LENGTH_MISMATCH)
            {
                Marshal.FreeHGlobal(ipObjectName);
                if (nLength == 0)
                {
                    Console.WriteLine("nLength returned at zero! " + strObjectTypeName);
                    return null;
                }
                ipObjectName = Marshal.AllocHGlobal(nLength);
            }
            objObjectName = (Win32API.OBJECT_NAME_INFORMATION)Marshal.PtrToStructure(ipObjectName, objObjectName.GetType());

            if (Is64Bits())
            {
                ipTemp = new IntPtr(Convert.ToInt64(objObjectName.Name.Buffer.ToString(), 10) >> 32);
            }
            else
            {
                ipTemp = objObjectName.Name.Buffer;
            }

            if (ipTemp != IntPtr.Zero)
            {

                var baTemp = new byte[nLength];
                try
                {
                    Marshal.Copy(ipTemp, baTemp, 0, nLength);

                    strObjectName = Marshal.PtrToStringUni(Is64Bits() ? new IntPtr(ipTemp.ToInt64()) : new IntPtr(ipTemp.ToInt32()));
                }
                catch (AccessViolationException)
                {
                    return null;
                }
                finally
                {
                    Marshal.FreeHGlobal(ipObjectName);
                    Win32API.CloseHandle(ipHandle);
                }
            }

            string path = GetRegularFileNameFromDevice(strObjectName);
            try
            {
                return path;
            }
            catch
            {
                return null;
            }
        }

        private string GetRegularFileNameFromDevice(string strRawName)
        {
            string strFileName = strRawName;
            foreach (string strDrivePath in Environment.GetLogicalDrives())
            {
                var sbTargetPath = new StringBuilder(Win32API.MAX_PATH);
                if (Win32API.QueryDosDevice(strDrivePath.Substring(0, 2), sbTargetPath, Win32API.MAX_PATH) == 0)
                {
                    return strRawName;
                }
                string strTargetPath = sbTargetPath.ToString();
                if (strFileName.StartsWith(strTargetPath))
                {
                    strFileName = strFileName.Replace(strTargetPath, strDrivePath.Substring(0, 2));
                    break;
                }
            }
            return strFileName;
        }

        private IEnumerable<Win32API.SYSTEM_HANDLE_INFORMATION> GetHandles(Process process)
        {
            var nHandleInfoSize = 0x10000;
            var ipHandlePointer = Marshal.AllocHGlobal(nHandleInfoSize);
            var nLength = 0;
            IntPtr ipHandle;

            while (Win32API.NtQuerySystemInformation(CNST_SYSTEM_HANDLE_INFORMATION, ipHandlePointer, nHandleInfoSize, ref nLength) == Win32API.STATUS_INFO_LENGTH_MISMATCH)
            {
                nHandleInfoSize = nLength;
                Marshal.FreeHGlobal(ipHandlePointer);
                ipHandlePointer = Marshal.AllocHGlobal(nLength);
            }

            var baTemp = new byte[nLength];
            Marshal.Copy(ipHandlePointer, baTemp, 0, nLength);

            long lHandleCount;
            if (Is64Bits())
            {
                lHandleCount = Marshal.ReadInt64(ipHandlePointer);
                ipHandle = new IntPtr(ipHandlePointer.ToInt64() + 8);
            }
            else
            {
                lHandleCount = Marshal.ReadInt32(ipHandlePointer);
                ipHandle = new IntPtr(ipHandlePointer.ToInt32() + 4);
            }

            var lstHandles = new List<Win32API.SYSTEM_HANDLE_INFORMATION>();

            for (long lIndex = 0; lIndex < lHandleCount; lIndex++)
            {
                var shHandle = new Win32API.SYSTEM_HANDLE_INFORMATION();
                if (Is64Bits())
                {
                    shHandle = (Win32API.SYSTEM_HANDLE_INFORMATION)Marshal.PtrToStructure(ipHandle, shHandle.GetType());
                    ipHandle = new IntPtr(ipHandle.ToInt64() + Marshal.SizeOf(shHandle) + 8);
                }
                else
                {
                    ipHandle = new IntPtr(ipHandle.ToInt64() + Marshal.SizeOf(shHandle));
                    shHandle = (Win32API.SYSTEM_HANDLE_INFORMATION)Marshal.PtrToStructure(ipHandle, shHandle.GetType());
                }
                if (shHandle.ProcessID != process.Id) continue;
                lstHandles.Add(shHandle);
            }
            return lstHandles;
        }

        private bool Is64Bits()
        {
            return Marshal.SizeOf(typeof(IntPtr)) == 8;
        }

        internal class Win32API
        {
            [DllImport("ntdll.dll")]
            public static extern int NtQueryObject(IntPtr ObjectHandle, int
                ObjectInformationClass, IntPtr ObjectInformation, int ObjectInformationLength,
                ref int returnLength);

            [DllImport("kernel32.dll", SetLastError = true)]
            public static extern uint QueryDosDevice(string lpDeviceName, StringBuilder lpTargetPath, int ucchMax);

            [DllImport("ntdll.dll")]
            public static extern uint NtQuerySystemInformation(int
                SystemInformationClass, IntPtr SystemInformation, int SystemInformationLength,
                ref int returnLength);

            [DllImport("kernel32.dll")]
            public static extern IntPtr OpenProcess(ProcessAccessFlags dwDesiredAccess, [MarshalAs(UnmanagedType.Bool)] bool bInheritHandle, int dwProcessId);
            [DllImport("kernel32.dll")]
            public static extern int CloseHandle(IntPtr hObject);
            [DllImport("kernel32.dll", SetLastError = true)]
            [return: MarshalAs(UnmanagedType.Bool)]
            public static extern bool DuplicateHandle(IntPtr hSourceProcessHandle,
               ushort hSourceHandle, IntPtr hTargetProcessHandle, out IntPtr lpTargetHandle,
               uint dwDesiredAccess, [MarshalAs(UnmanagedType.Bool)] bool bInheritHandle, uint dwOptions);
            [DllImport("kernel32.dll")]
            public static extern IntPtr GetCurrentProcess();

            public enum ObjectInformationClass
            {
                ObjectBasicInformation = 0,
                ObjectNameInformation = 1,
                ObjectTypeInformation = 2,
                ObjectAllTypesInformation = 3,
                ObjectHandleInformation = 4
            }

            [Flags]
            public enum ProcessAccessFlags : uint
            {
                All = 0x001F0FFF,
                Terminate = 0x00000001,
                CreateThread = 0x00000002,
                VMOperation = 0x00000008,
                VMRead = 0x00000010,
                VMWrite = 0x00000020,
                DupHandle = 0x00000040,
                SetInformation = 0x00000200,
                QueryInformation = 0x00000400,
                Synchronize = 0x00100000
            }

            [StructLayout(LayoutKind.Sequential)]
            public struct OBJECT_BASIC_INFORMATION
            { // Information Class 0
                public int Attributes;
                public int GrantedAccess;
                public int HandleCount;
                public int PointerCount;
                public int PagedPoolUsage;
                public int NonPagedPoolUsage;
                public int Reserved1;
                public int Reserved2;
                public int Reserved3;
                public int NameInformationLength;
                public int TypeInformationLength;
                public int SecurityDescriptorLength;
                public System.Runtime.InteropServices.ComTypes.FILETIME CreateTime;
            }

            [StructLayout(LayoutKind.Sequential)]
            public struct OBJECT_TYPE_INFORMATION
            { // Information Class 2
                public UNICODE_STRING Name;
                public int ObjectCount;
                public int HandleCount;
                public int Reserved1;
                public int Reserved2;
                public int Reserved3;
                public int Reserved4;
                public int PeakObjectCount;
                public int PeakHandleCount;
                public int Reserved5;
                public int Reserved6;
                public int Reserved7;
                public int Reserved8;
                public int InvalidAttributes;
                public GENERIC_MAPPING GenericMapping;
                public int ValidAccess;
                public byte Unknown;
                public byte MaintainHandleDatabase;
                public int PoolType;
                public int PagedPoolUsage;
                public int NonPagedPoolUsage;
            }

            [StructLayout(LayoutKind.Sequential)]
            public struct OBJECT_NAME_INFORMATION
            { // Information Class 1
                public UNICODE_STRING Name;
            }

            [StructLayout(LayoutKind.Sequential, Pack = 1)]
            public struct UNICODE_STRING
            {
                public ushort Length;
                public ushort MaximumLength;
                public IntPtr Buffer;
            }

            [StructLayout(LayoutKind.Sequential)]
            public struct GENERIC_MAPPING
            {
                public int GenericRead;
                public int GenericWrite;
                public int GenericExecute;
                public int GenericAll;
            }

            [StructLayout(LayoutKind.Sequential, Pack = 1)]
            public struct SYSTEM_HANDLE_INFORMATION
            { // Information Class 16
                public int ProcessID;
                public byte ObjectTypeNumber;
                public byte Flags; // 0x01 = PROTECT_FROM_CLOSE, 0x02 = INHERIT
                public ushort Handle;
                public int Object_Pointer;
                public UInt32 GrantedAccess;
            }

            public const int MAX_PATH = 260;
            public const uint STATUS_INFO_LENGTH_MISMATCH = 0xC0000004;
            public const int DUPLICATE_SAME_ACCESS = 0x2;
            public const uint FILE_SEQUENTIAL_ONLY = 0x00000004;
        }
        #endregion
    }
    #endregion
}