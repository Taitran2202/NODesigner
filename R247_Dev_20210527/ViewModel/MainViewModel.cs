using DevExpress.Mvvm;
using DevExpress.Mvvm.DataAnnotations;
using DevExpress.Mvvm.POCO;
using DevExpress.Xpf.Core;
using DynamicData;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NodeNetwork.ViewModels;
using NOVisionDesigner.Data;
using NOVisionDesigner.Designer;
using NOVisionDesigner.Designer.Misc;
using NOVisionDesigner.Designer.Nodes;
using NOVisionDesigner.UserControls;
using NOVisionDesigner.Windows;
using NOVisionDesigner.Windows.HelperWindows;
using ReactiveUI;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms.Integration;
using System.Windows.Input;
using System.Windows.Media;

namespace NOVisionDesigner.ViewModel
{
    public class MainViewModel : ReactiveObject,IDisposable
    {
        [ServiceProperty(Key = "ServiceWithCustomNotifications")]
        protected virtual INotificationService CustomNotificationService { get { return null; } }
        public NOVisionDesigner.Designer.Communication.HMIService HMIService;
        public AppSetting Setting { get; set; } = new AppSetting();
        public ServiceManagerViewModel ServiceManager { get; set; }
        static MainViewModel _instance;
        public static MainViewModel Instance 
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new MainViewModel();
                }
                return _instance;
            }
        } 
        public SynchronizationModel synchronizationModel { get; set; } = new SynchronizationModel();
        public UserViewModel UserViewModel { get; } = UserViewModel.Instance;
        public static EventHandler OnlineChanged;
        #region Field
        public UserModel currentUser;

        public CurrentUserRole CurrentRole;
        public Queue<SaveImageInfo> SaveImageQueue = new Queue<SaveImageInfo>();
        Queue<string> deleteFilesQueue = new Queue<string>();
        public RecordImageTool recordImage { get; set; } = new RecordImageTool();
        JobDetail _currentJob;
        public JobDetail CurrentJob
        {
            get
            {
                return _currentJob;
            }
            set
            {
                this.RaiseAndSetIfChanged(ref _currentJob, value);
            }
        }
        List<LogData> _status_history= new List<LogData>();
        public List<LogData> StatusHistory
        {
            get
            {
                return _status_history;
            }
            set
            {
                this.RaiseAndSetIfChanged(ref _status_history, value);
            }
        }
        LogData _status;
        public LogData Status
        {
            get
            {
                return _status;
            }
            set
            {
                this.RaiseAndSetIfChanged(ref _status, value);
            }
        }

        bool _loading;
        public bool Loading
        {
            get
            {
                return _loading;
            }
            set
            {
                this.RaiseAndSetIfChanged(ref _loading, value);
            }
        }
        string _loading_message;
        public string LoadingMessage
        {
            get
            {
                return _loading_message;
            }
            set
            {
                this.RaiseAndSetIfChanged(ref _loading_message, value);
            }
        }

        VisionModelManager _visionManger;
        public VisionModelManager VisionManager
        {
            get
            {
                return _visionManger;
            }
            set
            {
                this.RaiseAndSetIfChanged(ref _visionManger, value);
            }
        }
        bool _canOpenRecords;
        public bool CanOpenRecords
        {
            get
            {
                return _canOpenRecords;
            }
            set
            {
                this.RaiseAndSetIfChanged(ref _canOpenRecords, value);
            }
        }

        bool _canOpenDesigner; 
        public bool CanOpenDesigner
        {
            get
            {
                return _canOpenDesigner;
            }
            set
            {
                this.RaiseAndSetIfChanged(ref _canOpenDesigner, value);
            }
        }
        bool _canOpenMenu;
        public bool CanOpenMenu
        {
            get
            {
                return _canOpenMenu;
            }
            set
            {
                this.RaiseAndSetIfChanged(ref _canOpenMenu, value);
            }
        }
        bool _is_online;
        public bool IsOnline
        {
            get
            {
                return _is_online;
            }
            set
            {
                this.RaiseAndSetIfChanged(ref _is_online, value);
            }
        }


        //public List<SubCamStatistics> subcamStatistics = new List<SubCamStatistics>();

       
        public List<NodeViewModel> CopySelectedNodes { get; set; }
        public string CopyDir { get; set; }
        public ISourceList<ConnectionViewModel> CopyConnections { get; set; }
        #endregion

        #region Command
        public ICommand LoadedWindowCommand { get; set; }
        public ICommand LoginCommand { get; set; }
        public ICommand LogoutCommand { get; set; }
        public ICommand TestCommand { get; set; }
        public ICommand JobCommand { get; set; }
        public ICommand OnlineCommand { get; set; }
        public ICommand CreateUserCommand { get; set; }
        public ICommand PrivilegeCommand { get; set; }

        public ICommand StatisticsCommand { get; set; }
        public ICommand SaveCommand
        {
            get { return ReactiveCommand.Create((Control sender) => SaveJob()); }
        }
        public ICommand ShowJobControlCommand
        {
            get { return ReactiveCommand.Create((Control sender) => ShowJobControl(sender)); }
        }
        public ICommand CreateJobCommand
        {
            get { return ReactiveCommand.Create((Control sender) => CreateJobWindow()); }
        }
        public ICommand SaveACopyCommand
        {
            get { return ReactiveCommand.Create((Control sender) => SaveJobAs()); }
        }
        public ICommand AddVisionModelCommand
        {
            get { return ReactiveCommand.Create((Control sender) => AddVisionModel()); }
        }
        public ICommand ResetCommand
        {
            get { return ReactiveCommand.Create((Control sender) => ResetAllCounters()); }
        }

        #endregion
        public void ResetAllCounters()
        {
            if (Instance.VisionManager == null) { return; }
            foreach (var visionModel in Instance.VisionManager?.VisionModels)
            {
                foreach (AccquisitionNode acqNode in visionModel.Designer.Network.Nodes.Items.Where(x=>x is AccquisitionNode))
                {
                    acqNode.ResetCounter();
                }
            }
        }
        public void ShowCustomNotification()
        {
            NotificationViewModel vm = ViewModelSource.Create(() => new NotificationViewModel());
            vm.Caption = "Custom Notification";
            vm.Content = String.Format("Time: {0}", DateTime.Now);
            INotification notification = CustomNotificationService.CreateCustomNotification(vm);
            notification.ShowAsync();
        }
        public void AddVisionModel()
        {
            SaveWindow wd = new SaveWindow("Camera", "Enter vision name");
            if (wd.ShowDialog() == true)
            {
                var newItem = VisionManager?.AddVisionModel(wd.Text);
                newItem.Designer.UseDefaultLayout = true;
            }
        }
        public string PathJobs = Workspace.WorkspaceManager.Instance.jobPath;
        #region Construction Method
        public void OnLog(object sender, EventArgs e)
        {
            Status = sender as LogData;
            //StatusHistory.Add(Status);
        }
        public void LoadAppSetting()
        {
            try
            {
                var path = System.IO.Path.Combine(Workspace.WorkspaceManager.Instance.AppDataPath, "AppSetting.txt");
                Setting = JsonConvert.DeserializeObject<AppSetting>(File.ReadAllText(path));
            }catch(Exception ex)
            {
                Setting = new AppSetting();
            }

        }
        public async void LoadStartupJob()
        {
            if (Setting.StartupJob != null)
            {
                var startupjobpath = System.IO.Path.Combine(PathJobs, Setting.StartupJob);
                if (System.IO.Directory.Exists(startupjobpath))
                {
                    await LoadJob(new JobDetail(startupjobpath));
                    IsOnline = true;
                    OnlineCommand?.Execute(null);
                }
            }
            
        }
        private MainViewModel()
        {
            LoadAppSetting();
            //HMIService = new Designer.Communication.HMIService(this);
            try
            {
                var GlobalTagManger = GlobalTagManager.Instance;
                GlobalTagManger.Load();


            }
            catch (Exception ex)
            {
                App.GlobalLogger.LogError("ServiceManager", "Cannot initialize service manager: " + ex.Message);
            }
            try
            {
                ServiceManager = new ServiceManagerViewModel();
            }catch(Exception ex)
            {
                App.GlobalLogger.LogError("ServiceManager", "Cannot initialize service manager: " + ex.Message);
            }
            



            App.GlobalLogger.OnLog += OnLog;

            LoadedWindowCommand = new RelayCommand<object>((p) =>
            {
                return true;
            }, (p) =>
            {
                CheckCurrentUser(p as MainWindow);
                
            });

            StatisticsCommand = new RelayCommand<object>((p) =>
            {
                return true;
            }, (p) =>
            {
                TotalHMIWindow wd = new TotalHMIWindow(VisionManager);
                wd.Show();
            });

            LogoutCommand = new RelayCommand<object>((p) =>
            {
                return true;
            }, (p) =>
            {

                if (UserViewModel.LogOut())
                {
                    (p as MainWindow).btn_login.Visibility = Visibility.Visible;
                    (p as MainWindow).btn_logout.Visibility = Visibility.Collapsed;
                }
                CheckCurrentUser(p as MainWindow);

            });

            LoginCommand = new RelayCommand<object>((p) =>
            {
                return true;
            }, (p) =>
            {
                var loginScreen = new LoginScreen();
                //(loginScreen.DataContext as LoginScreenViewModel).mvm = this;
                loginScreen.ShowDialog();
                //(loginScreen.DataContext as LoginScreenViewModel).ShowManualDialog(loginScreen);
                //if (isAdmin)
                //{
                //    IsEditJob = true;
                //    IsEditTool = true;
                //    IsPrivilege = true;
                //    IsUserManagement = true;
                //}
                //else
                //{
                //    CheckCurrentUser();
                //}
                if (loginScreen.IsLoggedIn)
                {
                    (p as MainWindow).btn_login.Visibility = Visibility.Collapsed;
                    (p as MainWindow).btn_logout.Visibility = Visibility.Visible;
                }
                CheckCurrentUser(p as MainWindow);
            });

            CreateUserCommand = new RelayCommand<object>((p) =>
            {
                return true;
            }, (p) =>
            {
                //var createUser = new CreateUserWindow();
                //(createUser.DataContext as CreateUserViewModel).mvm = this;

                //(createUser.DataContext as CreateUserViewModel).ShowManualDialog(createUser);
            });

            PrivilegeCommand = new RelayCommand<object>((p) =>
            {
                return true;
            }, (p) =>
            {
                UserManagementWindow wd = new UserManagementWindow();
                wd.ShowDialog();
            });


            TestCommand = new RelayCommand<object>((p) =>
            {
                return true;
            }, (p) =>
            {
                //PropertyInfo[] infos = this.GetType().GetProperties();
                //foreach (PropertyInfo info in infos)
                //{
                //    string param_name = info.Name;
                //    object value = info.GetValue((p as MainWindow));
                //    if (value == null)
                //        continue;
                //}
            });
            JobCommand = new RelayCommand<object>((p) =>
            {
                return true;
            }, (p) =>
            {
                ShowJobControl(null);
            });

            OnlineCommand = new RelayCommand<object>((p) =>
            {
                return true;
            }, (p) =>
            {
                //ShowCustomNotification();
                OnlineChanged?.Invoke(IsOnline, null);
            });
            //LoadStartupJob();
        }

        #endregion

        #region Event Handler
        //private void Update_Image(object sender, PropertyChangedEventArgs e)
        //{
        //    (sender as System.Windows.Forms.PictureBox).Image = 
        //}
        #endregion

        #region Method
        public void SaveJob()
        {
            
            Task.Run(new Action(() =>
            {
                Loading = true;
                LoadingMessage = "saving job...";
                try
                {
                    if (CurrentJob == null)
                    {
                        return;
                    }
                    JObject data = new JObject();
                    string filepath = System.IO.Path.Combine(CurrentJob.Path, "config.json");
                    using (StreamWriter file = File.CreateText(filepath))
                    {
                        JsonSerializer serializer = new JsonSerializer();
                        serializer.Formatting = Formatting.Indented;
                        serializer.Serialize(file, data);
                    }
                    VisionManager?.Save();
                    OnSaveComplete?.Invoke(null, null);
                    App.GlobalLogger.LogInfo("Job",String.Format("Save job {0} successfully",CurrentJob.JobName));
                    
                }catch (Exception ex)
                {
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        DXMessageBox.Show("Error saving job", ex.Message, MessageBoxButton.OK, MessageBoxImage.Error);
                    });
                    //MessageBox.Show("Error saving job: " + ex.Message);
                }
                
                Loading = false;
                
                

            }));
            
            
            
        }
        public EventHandler OnLoadComplete;
        public EventHandler OnSaveComplete;
        public async Task<bool> LoadJob(string jobName,bool suppressDialog=false)
        {
            var JobPath = System.IO.Path.Combine(PathJobs, jobName);
            if (!System.IO.Directory.Exists(JobPath))
            {
                return false;
            }
            if (Loading)
            {
                return false;
            }

            return await Task.Run(() =>
            {
                Loading = true;
                LoadingMessage = "loading job...";
                try
                {

                    if (VisionManager != null)
                    {
                        IsOnline = false;
                        OnlineCommand?.Execute(null);
                        VisionManager.Dispose();
                    }
                    if (jobName != String.Empty)
                    {
                        CurrentJob = new JobDetail(JobPath);

                        var vision = new VisionModelManager(CurrentJob.Path);
                        vision.Load();
                        VisionManager = vision;
                        OnLoadComplete?.Invoke(null, null);
                        if (App.AppSetting.AutoOnlineJob)
                        {
                            IsOnline = false;
                            OnlineCommand?.Execute(true);
                            Thread.Sleep(100);
                            IsOnline = true;
                            OnlineCommand?.Execute(true);
                        }
                        
                        //MainWindow.AllowUIToUpdate();



                    }

                }
                catch (Exception ex)
                {
                    if (!suppressDialog)
                    {
                        Application.Current.Dispatcher.Invoke(() =>
                        {
                            DXMessageBox.Show("Error loading job", ex.Message, MessageBoxButton.OK, MessageBoxImage.Error);
                        });
                    }
                    else
                    {
                        App.GlobalLogger.LogError("Load Job", ex.Message);
                    }
                    return false;
                }
                Loading = false;
                return true;
            });


        }
        public async Task LoadJob(JobDetail job)
        {
            if (Loading)
            {
                return;
            }

            await Task.Run(new Action(() =>
            {
                Loading = true;
                LoadingMessage = "loading job...";
                try
                {

                    if (VisionManager != null)
                    {
                        IsOnline = false;
                        OnlineCommand?.Execute(null);
                        VisionManager.Dispose();
                    }
                    if (job != null)
                    {
                        CurrentJob = job;
                        
                        var vision = new VisionModelManager(CurrentJob.Path);
                        vision.Load();
                        VisionManager = vision;
                        OnLoadComplete?.Invoke(null, null);
                        //MainWindow.AllowUIToUpdate();
                        if (App.AppSetting.AutoOnlineJob)
                        {
                            IsOnline = false;
                            OnlineCommand?.Execute(true);
                            Thread.Sleep(100);
                            IsOnline = true;
                            OnlineCommand?.Execute(true);
                        }


                    }

                }
                catch (Exception ex)
                {
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        DXMessageBox.Show("Error loading job", ex.Message, MessageBoxButton.OK, MessageBoxImage.Error);
                    });
                }
                Loading = false;
            }));
            

        }
        public List<string> GetJobList()
        {
            return System.IO.Directory.GetDirectories(PathJobs).Select(x=>System.IO.Path.GetFileNameWithoutExtension(x)).ToList();
        }
        public void ShowJobControl(Control sender)
        {
            
            JobControlWindow wd = new JobControlWindow(PathJobs, true);
            if (sender != null)
            {
                try
                {
                    wd.Owner = Window.GetWindow(sender);
                }
                catch (Exception ex)
                {

                }
            }
            else
            {
                wd.Owner =Application.Current.MainWindow;
            }
            
            
            wd.ShowDialog();
        }
        private static void DirectoryCopy(string sourceDirName, string destDirName, bool copySubDirs)
        {
            // Get the subdirectories for the specified directory.
            DirectoryInfo dir = new DirectoryInfo(sourceDirName);

            if (!dir.Exists)
            {
                return;
            }

            DirectoryInfo[] dirs = dir.GetDirectories();

            // If the destination directory doesn't exist, create it.       
            Directory.CreateDirectory(destDirName);

            // Get the files in the directory and copy them to the new location.
            FileInfo[] files = dir.GetFiles();
            foreach (FileInfo file in files)
            {
                string tempPath = Path.Combine(destDirName, file.Name);
                file.CopyTo(tempPath, false);
            }

            // If copying subdirectories, copy them and their contents to new location.
            if (copySubDirs)
            {
                foreach (DirectoryInfo subdir in dirs)
                {
                    string tempPath = Path.Combine(destDirName, subdir.Name);
                    DirectoryCopy(subdir.FullName, tempPath, copySubDirs);
                }
            }
        }
        public void SaveJobAs()
        {
            
            if (CurrentJob == null)
                return;
            var joblist = System.IO.Directory.GetDirectories(Workspace.WorkspaceManager.Instance.jobPath).Select(d => new DirectoryInfo(d).Name).ToList();
            SaveWindow sw = new SaveWindow(CurrentJob.JobName+"_copy","Job name", joblist,"Job name already exist!");
            if (sw.ShowDialog() == true)
            {
                
                Task.Run(async () =>
                {
                    Loading = true;
                    LoadingMessage = "Copying job...";
                    var new_job_name = sw.Text;
                    string jobDir = "";
                    bool created = false;
                    try
                    {
                       
                        if (new_job_name == "")
                        {
                            jobDir = System.IO.Path.Combine(PathJobs, "untitled");
                        }
                        else { jobDir = System.IO.Path.Combine(PathJobs, new_job_name); }
                        if (System.IO.Directory.Exists(jobDir))
                        {
                            if (DXMessageBox.Show("This job is already existed. Do you want to replace it?", "Warning", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
                            {
                                System.IO.Directory.Delete(jobDir, true);
                                DirectoryCopy(CurrentJob.Path, jobDir, true);
                            }
                        }
                        else
                        {
                            DirectoryCopy(CurrentJob.Path, jobDir, true);

                        }
                        created = true;
                    }
                    catch(Exception ex)
                    {
                        Application.Current.Dispatcher.Invoke(() =>
                        {
                            DXMessageBox.Show("Some error has been occur: " + ex.Message,
                            "Error saving job", MessageBoxButton.OK, MessageBoxImage.Error);
                        });
                        
                        
                    }
                    Loading = false;
                    if (created)
                    {
                        await LoadJob(new JobDetail(jobDir));
                    }
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        DXMessageBox.Show("Job has been copy.");
                    });

                 });
            }
        }
        public async void CreateJobWindow()
        {
            var joblist = System.IO.Directory.GetDirectories(Workspace.WorkspaceManager.Instance.jobPath).Select(d => new DirectoryInfo(d).Name).ToList();
            SaveWindow sw = new SaveWindow("new_job","Job name" ,joblist, "Job name already exist!");
            if( sw.ShowDialog()== true)
            {
                var new_job_name = sw.Text;
                string jobDir = "";
                if (new_job_name =="")
                {
                    return;
                }
                else { jobDir = System.IO.Path.Combine(PathJobs, new_job_name); }
                if (System.IO.Directory.Exists(jobDir))
                {
                    if (DXMessageBox.Show("This job is already existed. Do you want to replace it?", "Warning", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
                    {
                        System.IO.Directory.Delete(jobDir,true);
                        if (System.IO.Directory.Exists(System.IO.Path.Combine(Workspace.WorkspaceManager.Instance.jobPath, "_temp_job_")))
                        {
                            System.IO.Directory.Move(System.IO.Path.Combine(Workspace.WorkspaceManager.Instance.jobPath, "_temp_job_"), jobDir);
                        }
                        else
                        {
                            System.IO.Directory.CreateDirectory(jobDir);
                        }
                    }
                    else
                    {
                        return;
                    }
                }
                else
                {
                    if (System.IO.Directory.Exists(System.IO.Path.Combine(Workspace.WorkspaceManager.Instance.jobPath, "_temp_job_")))
                    {
                        System.IO.Directory.Move(System.IO.Path.Combine(Workspace.WorkspaceManager.Instance.jobPath, "_temp_job_"), jobDir);
                    }
                    else
                    {
                        System.IO.Directory.CreateDirectory(jobDir);
                    }
                    
                }
                await LoadJob(new JobDetail(jobDir));
                var camera1 = VisionManager?.AddVisionModel("Camera 1");
                //Init Camera 1 View
                //var camera1 = VisionManager.VisionModels[0];
                camera1.Designer.UseDefaultLayout = true;
                DXMessageBox.Show(Application.Current.MainWindow, "Job has been created.","Info",MessageBoxButton.OK,MessageBoxImage.Information);
            }
        }
        public void SetDatacontextStatistics(int i, int j, StatisticsUCViewModel vm)
        {
        }
        public void CheckCurrentUser(MainWindow mainWindow)
        {
            var user = UserViewModel.CurrentUser;
            if (user.Role == null)
                return;
            if (user.Role.ID == 0 || Debugger.IsAttached)
            {
                using (var context = new UserContext())
                {
                    var user_permissions = context.RolePermissions.Include(x => x.Permission).Include(x => x.Role).Where(x => x.Role.ID == user.Role.ID).Select(x => x.Permission).ToList();
                    foreach (var permit in context.Permissions)
                    {
                        //var item = FindChild<UIElement>(mainWindow, permit.PermissionKey);
                        var item = mainWindow.FindName(permit.PermissionKey);
                        if (item is UIElement)
                        {
                            var control_assign = item as UIElement;
                            control_assign.IsEnabled = true;
                        }
                        //else if (ViewModel.PrivilegeViewModel.Instance.GetType().GetProperty(permit.PermissionKey) != null)
                        //{
                        //    var prop = ViewModel.PrivilegeViewModel.Instance.GetType().GetProperty(permit.PermissionKey);
                        //    prop.SetValue(ViewModel.PrivilegeViewModel.Instance, true, null);
                        //}
                    }
                }
                ViewModel.PrivilegeViewModel.Instance.EnableAll();
                return;
            }
            using (var context = new UserContext())
            {
                
                var user_permissions = context.RolePermissions.Include(x=>x.Permission).Include(x=>x.Role).Where(x=>x.Role.ID==user.Role.ID).Select(x=>x.Permission).ToList();
                foreach (var permit in context.Permissions)
                {
                    var item = mainWindow.FindName(permit.PermissionKey);
                    if (item is UIElement)
                    {
                        var control_assign = item as UIElement;
                        if(user_permissions != null)
                        {
                            if (user_permissions.Any(x => x.PermissionKey == permit.PermissionKey))
                            {
                                control_assign.IsEnabled = true;
                            }
                            else control_assign.IsEnabled = false;
                        }
                        
                    }
                    else if(ViewModel.PrivilegeViewModel.Instance.GetType().GetProperty(permit.PermissionKey)!=null)
                    {
                        var prop = ViewModel.PrivilegeViewModel.Instance.GetType().GetProperty(permit.PermissionKey);
                        if (user_permissions.Any(x => x.PermissionKey == permit.PermissionKey))
                        {
                            prop.SetValue(ViewModel.PrivilegeViewModel.Instance, true, null);
                        }
                        else prop.SetValue(ViewModel.PrivilegeViewModel.Instance, false, null);

                    }

                }
            }

        }
        //Task saveTask;
        //CancellationTokenSource saveTaskToken;
        //bool saveTaskRunning = false;
        
        public void Dispose()
        {
            OnlineChanged?.Invoke(false, null);
            VisionManager?.Dispose();
            // saveTaskRunning = false;
            //saveTask?.Wait(2000);
            //saveTaskToken?.Cancel();
            HMIService?.Dispose();
        }
        #endregion
    }
   
    public class AppSetting 
    {
        public void Save()
        {
            var path = System.IO.Path.Combine(Workspace.WorkspaceManager.Instance.AppDataPath, "AppSetting.txt");
            File.WriteAllText( path,JsonConvert.SerializeObject(this, new JsonSerializerSettings()
            {
                TypeNameHandling = TypeNameHandling.All
            }));
        }
        public AppSetting()
        {

        }
        string _default_record_path = Workspace.WorkspaceManager.Instance.RecordPath;
        public string DefaultRecordPath
        {
            get
            {
                return _default_record_path;
            }
            
        }
        string startup_job;
        
        public string StartupJob
        {
            get
            {
                return startup_job;
            }
            set
            {
                startup_job = value;
            }
        }

    }
    
}
