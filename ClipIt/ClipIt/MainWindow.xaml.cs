using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Diagnostics;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Collections;
using System.Collections.Specialized;
using System.IO;

using Unosquare.FFME;
using Unosquare.FFME.Common;

namespace ClipIt
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window, INotifyPropertyChanged
    {
        private TimeSpan videoDuration;

        private long? numVideoFrames;

        private double? frameRate;

        public event PropertyChangedEventHandler PropertyChanged;

        private bool LoopSegment;

        private string CurrMediaFilename;

        public bool IsPlaying { get; set; }

        private OrderedDictionary ffEncodersToCodecNames;

        private OrderedDictionary containerExtensionsToCommonNames;

        public MainWindow() {
            InitializeComponent();
            this.Loaded += MainWindow_Loaded;
            this.KeyDown += MainWindow_KeyDown;
            DataContext = this;

            ffEncodersToCodecNames = new OrderedDictionary();
            ffEncodersToCodecNames.Add("libx264", "H.264");
            ffEncodersToCodecNames.Add("libx265", "H.265/HEVC");
            ffEncodersToCodecNames.Add("mpeg4", "MPEG-4/DivX");

            CodecComboBox.ItemsSource = ffEncodersToCodecNames.Values;

            containerExtensionsToCommonNames = new OrderedDictionary();
            containerExtensionsToCommonNames.Add("mp4", "MP4");
            containerExtensionsToCommonNames.Add("mkv", "MKV (Matroska)");
            containerExtensionsToCommonNames.Add("avi", "AVI");
            containerExtensionsToCommonNames.Add("mov", "MOV (Quicktime)");
            containerExtensionsToCommonNames.Add("wmv", "WMV (Windows Media Video)");

            ContainerComboBox.ItemsSource = containerExtensionsToCommonNames.Values;

            Library.FFmpegDirectory = @"C:\ffmpeg-n4.4-79-gde1132a891-win64-gpl-shared-4.4\bin";

            FFMediaPlayer.PositionChanged += FFMediaPlayer_PositionChanged;
        }

        private void FFMediaPlayer_PositionChanged(object sender, PositionChangedEventArgs e) {
            long maxFrames = (long)((numVideoFrames ?? 0) * (ClipRangeSlider.UpperValue / ClipRangeSlider.Maximum));
            long currFrame = (long)(frameRate * ((Unosquare.FFME.MediaElement)sender).Position.TotalSeconds);

            if (currFrame >= maxFrames) {
                if (LoopSegment) {
                    UpdateVideoPosition();
                } else {
                    FFMediaPlayer.Stop();
                }
            }
        }

        // This method is called by the Set accessor of each property.  
        // The CallerMemberName attribute that is applied to the optional propertyName  
        // parameter causes the property name of the caller to be substituted as an argument.  
        private void NotifyPropertyChanged([CallerMemberName] String propertyName = "") {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private void MainWindow_KeyDown(object sender, KeyEventArgs e) {
            if (e.Key == Key.Left) {
                if (CanStepLeft()) {
                    FFMediaPlayer.StepBackward();
                }
            } else if (e.Key == Key.Right) {
                if (CanStepRight()) {
                    FFMediaPlayer.StepForward();
                }
            }
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e) {
            ClipRangeSlider.UpperValue = 100;
        }

        private async void LoadVideo(string filePath) {
            FFMediaPlayer.LoadedBehavior = MediaPlaybackState.Manual;
            FFMediaPlayer.UnloadedBehavior = MediaPlaybackState.Manual;

            await FFMediaPlayer.Open(new Uri(filePath));

            CurrMediaFilename = filePath;

            videoDuration = FFMediaPlayer.NaturalDuration ?? TimeSpan.Zero;

            frameRate = FFMediaPlayer.VideoFrameRate;

            numVideoFrames = (long) (frameRate * videoDuration.TotalSeconds);
        }

        private TimeSpan FramesToTime(long frames) {
            //Trace.WriteLine(string.Format("Frames: {0}", frames));
            //Trace.WriteLine(string.Format("Framerate: {0}", frameRate));
            return TimeSpan.FromSeconds(frames / frameRate ?? 29.97);
        }

        private long TimeToFrames(TimeSpan time) {
            return (long) (time.TotalSeconds * frameRate);
        }

        private bool CanStepLeft() {
            long minFrames = (long)((numVideoFrames ?? 0) * (ClipRangeSlider.LowerValue / ClipRangeSlider.Maximum));
            long currFrame = (long)(frameRate * FFMediaPlayer.Position.TotalSeconds);
            return currFrame > minFrames;
        }

        private bool CanStepRight() {
            long maxFrames = (long)((numVideoFrames ?? 0) * (ClipRangeSlider.UpperValue / ClipRangeSlider.Maximum));
            long currFrame = (long)(frameRate * FFMediaPlayer.Position.TotalSeconds);
            return currFrame < maxFrames;
        }

        private async void SnapIntoRange() {
            long minFrames = (long)((numVideoFrames ?? 0) * (ClipRangeSlider.LowerValue / ClipRangeSlider.Maximum));
            long maxFrames = (long)((numVideoFrames ?? 0) * (ClipRangeSlider.UpperValue / ClipRangeSlider.Maximum));

            long currFrame = (long)(frameRate * FFMediaPlayer.Position.TotalSeconds);
            
            if (currFrame < minFrames) {
                FFMediaPlayer.Seek(FramesToTime(minFrames));
            } else if (currFrame > maxFrames) {
                FFMediaPlayer.Seek(FramesToTime(maxFrames));
            }
        }

        private async void UpdateVideoPosition() {
            long seekFrames = (long) ((numVideoFrames ?? 0) * (ClipRangeSlider.LowerValue / ClipRangeSlider.Maximum));
            
            if (seekFrames == 0L) {
                FFMediaPlayer.Stop();
            } else {
                FFMediaPlayer.Seek(FramesToTime(seekFrames));
            }
        }

        private void ClipRangeSlider_UpperValueChanged(object sender, EventArgs e) {
            //Trace.WriteLine("Upper value changed!");
            //maxFrames = (long)((numVideoFrames ?? 0) * (ClipRangeSlider.UpperValue / ClipRangeSlider.Maximum));
        }

        private void ClipRangeSlider_LowerValueChanged(object sender, EventArgs e) {
            //Trace.WriteLine("Lower value changed!");
            if (FFMediaPlayer.Source != null) {
                UpdateVideoPosition();
            }
        }

        private void OpenMedia_Click(object sender, RoutedEventArgs e) {
            Microsoft.Win32.OpenFileDialog dlg = new Microsoft.Win32.OpenFileDialog();
            dlg.FileName = "video"; // Default file name
            dlg.DefaultExt = ".mp4"; // Default file extension
            dlg.Filter = "Video Files|*.mp4;*.mkv;*.avi"; // Filter files by extension

            // Show open file dialog box
            bool? result = dlg.ShowDialog();

            // Process open file dialog box results
            if (result == true) {
                // Open document
                string filename = dlg.FileName;

                ClipRangeSlider.LowerValue = 0;
                ClipRangeSlider.UpperValue = 100;
                LoadVideo(filename);
            }
        }

        private void Exit_Click(object sender, RoutedEventArgs e) {
            Environment.Exit(0);
        }

        private void PlayPause_Click(object sender, RoutedEventArgs e) {
            if (IsPlaying) {
                FFMediaPlayer.Pause();
            } else {
                FFMediaPlayer.Play();
            }
            IsPlaying = !IsPlaying;

            NotifyPropertyChanged("IsPlaying");
        }

        private void LoopCheckBox_Checked(object sender, RoutedEventArgs e) {
            LoopSegment = LoopCheckBox.IsChecked ?? false;
        }

        private void ExportButton_Click(object sender, RoutedEventArgs e) {
            DictionaryEntry selectedContainer = containerExtensionsToCommonNames.Cast<DictionaryEntry>().ElementAt(ContainerComboBox.SelectedIndex);

            Microsoft.Win32.SaveFileDialog dlg = new Microsoft.Win32.SaveFileDialog();
            dlg.FileName = "out_video"; // Default file name
            dlg.DefaultExt = string.Format(".{0}", selectedContainer.Key); // Default file extension
            dlg.Filter = string.Format("Video File|*.{0}", selectedContainer.Key); // Filter files by extension

            // Show open file dialog box
            bool? result = dlg.ShowDialog();

            // Process open file dialog box results
            if (result == true) {
                // Open document
                string outFilename = dlg.FileName;
                
                DictionaryEntry selectedCodec = ffEncodersToCodecNames.Cast<DictionaryEntry>().ElementAt(CodecComboBox.SelectedIndex);

                TimeSpan startTime = videoDuration * (ClipRangeSlider.LowerValue / ClipRangeSlider.Maximum);

                string startTimeStr = startTime.ToString(@"hh\:mm\:ss\.ff");
                string endTime = ((videoDuration * (ClipRangeSlider.UpperValue / ClipRangeSlider.Maximum)) - startTime).ToString(@"hh\:mm\:ss\.ff");

                string ffmpegArgs = string.Format(" -ss {0} -i \"{1}\" -to {2} -c:v {3} -crf 18 -c:a aac \"{4}\"", startTime, CurrMediaFilename, endTime, selectedCodec.Key, outFilename);

                Process ffmpeg = new Process();
                ffmpeg.StartInfo.FileName = "ffmpeg.exe";
                ffmpeg.StartInfo.Arguments = ffmpegArgs;
                ffmpeg.StartInfo.RedirectStandardInput = true;
                ffmpeg.StartInfo.CreateNoWindow = true;
                ffmpeg.Start();

                if (File.Exists(outFilename)) {
                    var streamWriter = ffmpeg.StandardInput;
                    streamWriter.WriteLine("y");
                }

                Trace.WriteLine(ffmpegArgs);
            }
        }

        private void CodecComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e) {
            if (CodecComboBox.SelectedItem == null || ContainerComboBox.SelectedItem == null) {
                ExportButton.IsEnabled = false;
            } else {
                ExportButton.IsEnabled = true;
            }
        }

        private void ContainerComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e) {
            if (CodecComboBox.SelectedItem == null || ContainerComboBox.SelectedItem == null) {
                ExportButton.IsEnabled = false;
            } else {
                ExportButton.IsEnabled = true;
            }
        }
    }
}
