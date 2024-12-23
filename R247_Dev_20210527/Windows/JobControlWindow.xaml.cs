using DevExpress.Xpf.Core;
using DevExpress.Xpf.Editors;
using Newtonsoft.Json.Linq;
using NOVisionDesigner.ViewModel;
using NOVisionDesigner.Windows.HelperWindows;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reactive.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using ReactiveUI;
using System.Reactive.Concurrency;
using System.IO;
using Microsoft.WindowsAPICodePack.Dialogs;

namespace NOVisionDesigner.Windows
{
    /// <summary>
    /// Interaction logic for JobControlWindow.xaml
    /// </summary>

    public partial class JobControlWindow : ThemedWindow
    {
        string PathJobs;
        List<string> SortByList = new List<string>() { "Modified Date", "Name" };
        private ICollectionView View;
        public JobControlWindow(string PathJobs, bool job_advance)
        {
            InitializeComponent();
            this.PathJobs = PathJobs;
            cmb_sort.ItemsSource = SortByList;
            cmb_sort.SelectionChanged += Cmb_sort_SelectionChanged;
            lst_jobs.Items.SortDescriptions.Add(new SortDescription("LastModifiedDate", ListSortDirection.Descending));
            View = CollectionViewSource.GetDefaultView(lst_jobs.ItemsSource);
            var fswCreated = Observable.FromEvent<EditValueChangedEventHandler, DevExpress.Xpf.Editors.EditValueChangedEventArgs>(handler =>
            {
                EditValueChangedEventHandler fsHandler = (sender, e) =>
                {
                    handler(e);
                };

                return fsHandler;
            },
            fsHandler => txtFilter.EditValueChanged += fsHandler,
            fsHandler => txtFilter.EditValueChanged -= fsHandler);
            fswCreated
                    .Throttle(TimeSpan.FromMilliseconds(600)).ObserveOn(Application.Current.Dispatcher).Subscribe(e => {
                        if (e.NewValue != null)
                        {
                            View.Filter = x => (x as JobDetail).JobName.ToLower().Contains(e.NewValue.ToString().ToLower());
                        }
                        else
                        {
                            View.Filter = null;
                        }

                    });
        }

        private void TxtFilter_EditValueChanged(object sender, DevExpress.Xpf.Editors.EditValueChangedEventArgs e)
        {
            
            
        }

        private void Cmb_sort_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var selected = e.AddedItems[0].ToString();
            if (selected== "Name")
            {
                lst_jobs.Items.SortDescriptions.Clear();
                lst_jobs.Items.SortDescriptions.Add(new SortDescription("JobName", ListSortDirection.Ascending));
            }
            else
            {
                lst_jobs.Items.SortDescriptions.Clear();
                lst_jobs.Items.SortDescriptions.Add(new SortDescription("LastModifiedDate", ListSortDirection.Descending));
            }
        }

        private object discover_lock = new object();
        private void RefreshJobList(string PathJob)
        {
            loading.DeferedVisibility = true;
            Task.Run(new Action(() =>
            {
                lock (discover_lock)
                {
                    List<JobDetail> list_loaded_jobs = new List<JobDetail>();
                    foreach (var jobPath in System.IO.Directory.GetDirectories(PathJob).ToList())
                    {
                        JobDetail jobDetail = new JobDetail(jobPath);
                        if(jobDetail.JobName== MainViewModel.Instance.Setting.StartupJob)
                        {
                            jobDetail.IsStartup = true;
                        }
                        list_loaded_jobs.Add(jobDetail);
                    }

                    Application.Current.Dispatcher.BeginInvoke(new Action(() =>
                    {
                        lst_jobs.ItemsSource = list_loaded_jobs;
                        View = CollectionViewSource.GetDefaultView(lst_jobs.ItemsSource);
                        loading.DeferedVisibility = false;
                    }
                    ));
                }
            }));
        }
        private void Btn_new_job_Click(object sender, RoutedEventArgs e)
        {
            MainViewModel.Instance.CreateJobWindow();
            RefreshJobList(PathJobs);
        }

        private void btn_ribbon_rename_jobs_Click(object sender, RoutedEventArgs e)
        {
            if(DXMessageBox.Show("Do you want to rename this job?","Rename Job", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
            {
                if(lst_jobs.SelectedItem is JobDetail temp)
                {
                    SaveWindow sw = new SaveWindow();
                    if(sw.ShowDialog()== true)
                    {

                        var newjob = new JobDetail(Workspace.WorkspaceManager.Instance.GenJobPath(sw.Text));
                        if (System.IO.Directory.Exists(newjob.Path))
                        {
                            System.IO.Directory.Delete(newjob.Path);
                        }
                        System.IO.Directory.Move(temp.Path, newjob.Path);
                        MainViewModel.Instance.CurrentJob = newjob;
                        (lst_jobs.SelectedItem as JobDetail).JobName = newjob.JobName;
                        (lst_jobs.SelectedItem as JobDetail).Path = newjob.Path;
                        lst_jobs.Items.Refresh();
                        
                    }
                    
                    
                }
            }
        }
        private void ThemedWindow_ContentRendered(object sender, EventArgs e)
        {
            RefreshJobList(PathJobs);
        }

        private void btn_ribbon_delete_jobs_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (DXMessageBox.Show("Do you want to delete this job?", "Delete Job", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
                {
                    if (lst_jobs.SelectedItem is JobDetail temp)
                    {
                        if (System.IO.Directory.Exists(temp.Path))
                        {
                            SaveWindow.DeleteDirectory(temp.Path);
                            if (MainViewModel.Instance.CurrentJob == temp) { MainViewModel.Instance.CurrentJob = null; }
                            (lst_jobs.ItemsSource as List<JobDetail>).Remove(temp);
                            lst_jobs.Items.Refresh();
                        }
                    }
                }
            }
            catch (Exception ex) { DXMessageBox.Show(ex.Message); }
        }

        private void lst_jobs_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (lst_jobs.SelectedItem == null)
            {
                btn_rename.IsEnabled = false;
                btn_delete.IsEnabled = false;
            }
            else
            {
                btn_rename.IsEnabled = true;
                btn_delete.IsEnabled = true;
            }
        }

        private void menu_edit_Click(object sender, RoutedEventArgs e)
        {

        }

        private void btn_load_inside_Click(object sender, RoutedEventArgs e)
        {
            MainViewModel.Instance.LoadJob(lst_jobs.SelectedItem as JobDetail);
            this.Close();
        }

        private void btn_open_job_directory_Click(object sender, RoutedEventArgs e)
        {
            System.Diagnostics.Process.Start("explorer.exe",
                System.IO.Path.Combine(MainViewModel.Instance.PathJobs));
        }

        private void btn_setstartup_Click(object sender, RoutedEventArgs e)
        {
            MainViewModel.Instance.Setting.StartupJob = (lst_jobs.SelectedItem as JobDetail).JobName;
            foreach(var item in lst_jobs.ItemsSource)
            {
                (item as JobDetail).IsStartup = false;
            }
            (lst_jobs.SelectedItem as JobDetail).IsStartup=true;
            MainViewModel.Instance.Setting.Save();
            View?.Refresh();
        }

        private void btn_unsetstartup_Click(object sender, RoutedEventArgs e)
        {
            MainViewModel.Instance.Setting.StartupJob = null;
            (lst_jobs.SelectedItem as JobDetail).IsStartup = false;
            MainViewModel.Instance.Setting.Save();
            View?.Refresh();
        }

        private void btn_open_remote_job_Click(object sender, RoutedEventArgs e)
        {
            DiscoveryWindow wd = new DiscoveryWindow();
            wd.ShowDialog();
        }

        private void btn_browse_job_Click(object sender, RoutedEventArgs e)
        {
            using (var dialog = new CommonOpenFileDialog())
            {
                dialog.IsFolderPicker = true;
                CommonFileDialogResult result = dialog.ShowDialog();
                if (result == CommonFileDialogResult.Ok)
                {
                    var job = new JobDetail(dialog.FileName);
                    this.Close();
                    MainViewModel.Instance.LoadJob(job);
                    
                }
            }
        }
    }

    public class JobDetail 
    {
        public override string ToString()
        {
            return JobName;
        }
        public JobDetail()
        {
            JobName = "Unknow";
            LastModifiedDate = DateTime.Now;
            LastModifiedDateString = LastModifiedDate.ToString("HH:mm   dd/MM/yyy");
            Path = "Unknow";
            Version = "";
        }
        public JobDetail(string jobPath)
        {
            JobName = System.IO.Path.GetFileNameWithoutExtension(jobPath);
            LastModifiedDate = GetLastModifiedDate(jobPath);
            CreatedDateString = System.IO.Directory.GetCreationTime(jobPath).ToString("HH:mm   dd/MM/yyy");
            LastModifiedDateString = LastModifiedDate.ToString("HH:mm   dd/MM/yyy");
            Path = jobPath;
            Version = "";
        }
        public DateTime GetLastModifiedDateV2(string dir)
        {
            var folder = new DirectoryInfo(dir);
            var files = folder.EnumerateFiles("*.*", SearchOption.AllDirectories);
            return files.OrderBy(x => x.LastWriteTime).Last().LastWriteTime;
        }
        public DateTime GetLastModifiedDate(string dir)
        {
            if(System.IO.File.Exists(System.IO.Path.Combine(dir, "config.json")))
            {
                return System.IO.Directory.GetLastWriteTime(System.IO.Path.Combine(dir, "config.json"));
            }
            else
            {
                return System.IO.Directory.GetLastWriteTime(dir);
            }
            
        }
        public string JobName { get; set; }
        public string Path { get; set; }
        public DateTime LastModifiedDate { get; set; }
        public string LastModifiedDateString { get; set; }
        public string CreatedDateString { get; set; }
        public string Version { get; set; }
        public bool IsStartup { get; set; }
    }
}
