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

        private int numberOfSpectrumBars = 21;

        private bool isShuffle = false;
        private bool isRepeat = false;

        private Color inactiveColor = Color.FromRgb(146, 170, 179);
        private Color activeColor = Color.FromRgb(69, 206, 254);

        AudioFileReader naudio;
        WaveOut waveOut = new WaveOut();

        FFTSampleProvider sampProv;


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


            //DrawHeart();

            OnWindowActive(null, null);

            //waveOut.PlaybackStopped += WaveOut_PlaybackStopped;
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

                polilineHeart.Points.Add(new Point(Width / 2  - x * 10, Height / 2 - 20 - y * 10));
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

            naudio = new AudioFileReader(Songs[index].path);

            sampProv = new FFTSampleProvider(naudio);


            waveOut = new WaveOut();

            sampProv.FFT_Calculated += SampProv_FFT_Calculated;

            waveOut.PlaybackStopped += WaveOut_PlaybackStopped;
            

            waveOut.Init(sampProv);
            waveOut.Play();

            songLabel.Content = Songs[index].name;



            currentSongIndex = index;
        }

        private void SampProv_FFT_Calculated(object sender, FFT_EventArgs e)
        {
            //for (int i = 0; i < 50; i ++)
            //{
            //    var value = (10 * Math.Log10(Math.Sqrt(e.Result[i].X * e.Result[i].X + e.Result[i].Y * e.Result[i].Y)));
            //    Console.WriteLine( (1- 0) / ( 0 + 60) * (value - 0) + 1);
            //}

            polilineFFT.Points.Clear();
            backpolilineFFT.Points.Clear();

            for (int i = 0; i < e.Result.Length; i = i + 3)
            {
                var value = (10 * Math.Log(Math.Sqrt(e.Result[i].X * e.Result[i].X + e.Result[i].Y * e.Result[i].Y)));
                var nValue = (1 - 0.0) / (0 + 90.0) * (value - 0.0) + 1.0;

                //polilineFFT.Points.Add(new Point(Width / 4 +  Width / 2 * i / e.Result.Length, Height / 2 - nValue * 50)); //pentru iubi
                polilineFFT.Points.Add(new Point(20 + (Width - 40) * i / (e.Result.Length), Height / 2 - nValue * 50));

                if (i <= 400)
                {
                    backpolilineFFT.Points.Add(new Point(20 + i * (ActualWidth - 40) / 400, 200 - (nValue * 50)));
                }
            }

            this.InvalidateVisual();

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
                ShowPauseButton();
            }
        }

        private void PlayPauseButton (object sender, MouseButtonEventArgs e)
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
                for (int i = 0; i < Songs.Count; i ++)
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
                    while(string.IsNullOrEmpty((line = sr.ReadLine())) == false)
                    {
                        if (string.IsNullOrEmpty(line) == false && File.Exists(line) == true)
                        {
                            Songs.Add(new Song() { name = System.IO.Path.GetFileNameWithoutExtension(line), path = line }); //duration = (new AudioFileReader(line).TotalTime) });
                        }
                    }
                }
                File.Delete("resources/list.txt");

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
            waveOut.Volume = (float)(slider.Value / 100.0);
        }
    }
}
