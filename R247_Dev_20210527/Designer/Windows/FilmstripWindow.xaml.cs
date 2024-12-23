using NOVisionDesigner.Designer.Misc;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using Microsoft.WindowsAPICodePack.Dialogs;
using DevExpress.Xpf.Core;

namespace NOVisionDesigner.Designer.Windows
{
    /// <summary>
    /// Interaction logic for FilmstripWindow.xaml
    /// </summary>
    public partial class FilmstripWindow : ThemedWindow, INotifyPropertyChanged
    {

        void RaisePropertyChanged(string prop)
        {
            if (PropertyChanged != null) { PropertyChanged(this, new PropertyChangedEventArgs(prop)); }
        }
        public event PropertyChangedEventHandler PropertyChanged;

        public List<ImageFilmstrip> lst_images = new List<ImageFilmstrip>();
        Accquisition.Accquisition acq;
        //  System.Windows.Threading.DispatcherTimer timer = new System.Windows.Threading.DispatcherTimer(System.Windows.Threading.DispatcherPriority.SystemIdle);
        public FilmstripWindow(Accquisition.Accquisition acq)
        {
            this.acq = acq;
            InitializeComponent();
            stack_controls.DataContext = this;
            this.Closing += FilmstripWindow_Closing;
            //   timer.Interval = TimeSpan.FromMilliseconds(200);
            //   timer.Tick += OnTimer;

        }

        private void FilmstripWindow_Closing(object sender, CancelEventArgs e)
        {
            IsRun = false;
            //if (runTask != null)
            //{
            //    if (!runTask.IsCompleted)
            //    {
            //        runTask.Wait(2000);
            //    }
            //    if (!runTask.IsCompleted)
            //    {
            //        runTask.
            //    }
            //}
        }

        public void OnTimer(object sender, EventArgs e)
        {
            //  timer.Stop();
            lst_view.SelectedIndex++;
            lst_view.ScrollIntoView(lst_view.SelectedItem);


        }
        bool is_run;
        public bool IsRun
        {
            get
            {
                return is_run;
            }
            set
            {
                if (is_run != value)
                {
                    is_run = value;
                    RaisePropertyChanged("IsRun");
                }
            }
        }

        int _delay = 100;
        public int Delay
        {
            get
            {
                return _delay;
            }
            set
            {
                if (_delay != value)
                {
                    if (value > 20)
                    {
                        _delay = value;
                        RaisePropertyChanged("Delay");
                    }
                }
            }
        }

        private void btn_select_directory_Click(object sender, RoutedEventArgs e)
        {

            CommonOpenFileDialog dialog = new CommonOpenFileDialog();
            dialog.IsFolderPicker = true;
            //diag.folder
            // diag.SelectedPath = acq.Record_path;
            if (dialog.ShowDialog() == CommonFileDialogResult.Ok)
            {
                lst_images.Clear();
                string[] files = Directory.GetFiles(dialog.FileName, "*.bmp").Union(Directory.GetFiles(dialog.FileName, "*.jpg")).Union(Directory.GetFiles(dialog.FileName, "*.png")).Union(Directory.GetFiles(dialog.FileName, "*.tif")).ToArray();
                foreach (string file in files)
                {

                    lst_images.Add(new ImageFilmstrip(file));
                }
                lst_view.ItemsSource = null;
                lst_view.ItemsSource = lst_images;

            }
        }
        Task runTask = null;
        private void btn_play_Click(object sender, RoutedEventArgs e)
        {

            IsRun = true;
            runTask=Task.Run(new Action(() =>
            {
                Thread.CurrentThread.Priority = ThreadPriority.BelowNormal;
                while (is_run)
                {
                    ImageFilmstrip selected = null;
                    var aa = Application.Current.Dispatcher.BeginInvoke(new Action(() =>
                    {
                        if (lst_view.SelectedIndex == lst_view.Items.Count - 1)
                        {
                            lst_view.SelectedIndex = 0;
                        }
                        else
                        {
                            lst_view.SelectedIndex++;
                        }

                        selected = lst_view.SelectedItem as ImageFilmstrip;
                        lst_view.ScrollIntoView(selected);
                    }));
                    aa.Wait();

                    //foreach (ImageFilmstrip film in lst_images)
                    //{
                    //    acq.OpenImage(film.FullPath);
                    //}

                    Thread.Sleep(Delay);
                }
            }));
        }

        private void lst_view_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ImageFilmstrip selected = lst_view.SelectedItem as ImageFilmstrip;
            if (selected != null)
            {
                acq.OpenImage(selected.FullPath);
            }
            // timer.Start();
        }

        private void btn_stop_Click(object sender, RoutedEventArgs e)
        {
            IsRun = false;
        }

        private void MetroWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            IsRun = false;
        }

        private void btn_next_Click(object sender, RoutedEventArgs e)
        {
            lst_view.SelectedIndex++;
            ImageFilmstrip selected = lst_view.SelectedItem as ImageFilmstrip;
            lst_view.ScrollIntoView(selected);
        }

        private void btn_pre_Click(object sender, RoutedEventArgs e)
        {
            if (lst_view.SelectedIndex > 0)
            {
                lst_view.SelectedIndex--;
                ImageFilmstrip selected = lst_view.SelectedItem as ImageFilmstrip;
                lst_view.ScrollIntoView(selected);
            }
        }

        private void Border_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            this.DragMove();
        }

        private void btn_close_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.MainWindow.Activate();
            this.Close();
        }

        private void MetroWindow_Initialized(object sender, EventArgs e)
        {
            try
            {
                this.Owner = Application.Current.MainWindow;
                this.ShowInTaskbar = false;
            }
            catch (Exception ex)
            {

            }
        }

        private void MetroWindow_ContentRendered(object sender, EventArgs e)
        {
            //   this.Owner = Application.Current.MainWindow;
        }
    }

}
