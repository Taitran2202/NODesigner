using Basler.Pylon;
using DevExpress.Xpf.Bars;
using DevExpress.Xpf.Core;
using DevExpress.Xpf.Docking;
using DynamicData;
using HalconDotNet;
using MahApps.Metro.Controls;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NodeNetwork.ViewModels;
using NOVision.Windows;
using NOVisionDesigner.Designer;
using NOVisionDesigner.Designer.GroupNodes;
using NOVisionDesigner.Designer.Nodes;
using NOVisionDesigner.UserControls;
using NOVisionDesigner.ViewModel;
using NOVisionDesigner.Windows;
using ReactiveUI;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
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
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace NOVisionDesigner
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : ThemedWindow
    {
        public static bool is_load;
        public static string AppPath = AppDomain.CurrentDomain.BaseDirectory;
        public static string current_program_name="not_yet_implement";
        #region Field
        public static int pixelWidth;
        public static int pixelHeight;
        public static double pixelDPI;
        public static double widthmain = 0;
        public static double heigthman = 0;
        
        #endregion

        public async void CheckforUpdate()
        {
            //try
            //{
            //    using (var mgr = new UpdateManager(@"D:\GitHub\R247\NOVisionDesigner\Releases"))
            //    {
            //        await mgr.UpdateApp();
            //    }
            //}catch (Exception ex)
            //{
            //    MessageBox.Show(ex.Message);
            //}
            
        }
        public string GetVersion()
        {
            Assembly assembly = Assembly.GetExecutingAssembly();
            FileVersionInfo fileVersionInfo = FileVersionInfo.GetVersionInfo(assembly.Location);
            return fileVersionInfo.FileVersion;
        }
        public static string APPVERSION;
        void Window1_SourceInitialized(object sender, EventArgs e)
        {
            if (App.AppSetting.ShowTaskBarFullScreen)
            {
                WindowSizing.WindowInitialized(this);
            }
            
        }
        public MainWindow()
        {
            InitializeComponent();
            this.SourceInitialized += new EventHandler(Window1_SourceInitialized);
            this.DataContext = MainViewModel.Instance;
            MainViewModel.Instance.OnLoadComplete += OnLoadComplete;
            MainViewModel.Instance.OnSaveComplete += OnSaveComplete;
            loading_static = loading;
            lb_version.Content = GetVersion();
            APPVERSION = GetVersion();
            if (!Debugger.IsAttached)
            {
                if (LicenceManager.Licence.Instance.ValidateLicence())
                {
                    App.splash.CloseSplash();
                }
                else
                {
                    ValidateLicenceWindow wd = new ValidateLicenceWindow();
                    if (wd.ShowDialog() != true)
                    {
                        App.splash.CloseSplash();
                        this.Close();
                        return;
                    }


                }
            }
            else
            {
                App.splash.CloseSplash();
            }
            
            //CheckforUpdate();
            
        }
        public static void AllowUIToUpdate()
        {

            DispatcherFrame frame = new DispatcherFrame();

            Dispatcher.CurrentDispatcher.BeginInvoke(DispatcherPriority.Render, new DispatcherOperationCallback(delegate (object parameter)

            {

                frame.Continue = false;

                return null;

            }), null);

            Dispatcher.PushFrame(frame);

        }
        public void OnLoadComplete(object sender,EventArgs e)
        {
            Thread.Sleep(2000);
            RestoreLayout();
            if (App.AppSetting.SetLoadJobAsStartup)
            {
                try
                {
                    MainViewModel.Instance.Setting.StartupJob = MainViewModel.Instance.CurrentJob.JobName;
                    MainViewModel.Instance.Setting.Save();
                }catch(Exception ex)
                {

                }
                
                //App.AppSetting.StartupJob
            }
            //Thread.Sleep(2000);


        }

        private void RestoreLayout()
        {
            if (MainViewModel.Instance.CurrentJob == null)
                return;
            var dir = MainViewModel.Instance.CurrentJob.Path;
            var layoutpath = System.IO.Path.Combine(dir, "layout.xml");
            if (System.IO.File.Exists(layoutpath))
            {
                Application.Current.Dispatcher.Invoke(new Action(() =>
                {
                    //docklayout.items();

                    try
                    {
                        //docklayout.InvalidateVisual();
                        //AllowUIToUpdate();

                        docklayout.RestoreLayoutFromXml(layoutpath);
                    }
                    catch (Exception ex)
                    {

                    }

                }));
            }
        }


        public void OnSaveComplete(object sender, EventArgs e)
        {
            if (MainViewModel.Instance.CurrentJob == null)
                return;
            var dir = MainViewModel.Instance.CurrentJob.Path;
            var layoutpath = System.IO.Path.Combine(dir, "layout.xml");
            Application.Current.Dispatcher.BeginInvoke(new Action(() =>
            {
                docklayout.SaveLayoutToXml(layoutpath);
            }));
            
        }
        static WaitIndicator loading_static;
        public static void ShowLoading(string message)
        {
            Application.Current.Dispatcher.Invoke(new Action(() =>
            {
                loading_static.Content = message;
                loading_static.DeferedVisibility = true;
            }));
            
        }
        public static void CloseLoading()
        {
            Application.Current.Dispatcher.Invoke(new Action(() =>
            {
                loading_static.DeferedVisibility = false;
            }));
        }
        private void MainWD_Loaded(object sender, RoutedEventArgs e)
        {
        }
       

        private void btn_save_job_Click(object sender, RoutedEventArgs e)
        {
            //SaveJob();
            //DXMessageBox.Show("Job has been saved.");
        }
        
        private void btn_reset_order_Click(object sender, RoutedEventArgs e)
        {
            //VisionManager.AddVisionModel();
        }

        private void btn_expander_Click(object sender, RoutedEventArgs e)
        {
            
            MainViewModel.Instance.Dispose();
            this.Close();
            
        }

        private void btn_minimize_Click(object sender, RoutedEventArgs e)
        {
            this.WindowState = WindowState.Minimized;
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {

        }

        private void btn_controller_Click_1(object sender, DevExpress.Xpf.Bars.ItemClickEventArgs e)
        {
            RestoreLayout();
        }
        private void btn_save_layout_click(object sender, DevExpress.Xpf.Bars.ItemClickEventArgs e)
        {
            OnSaveComplete(null, null);
        }

        private void docklayout_DockItemClosed(object sender, DevExpress.Xpf.Docking.Base.DockItemClosedEventArgs e)
        {
            
            
        }

        private void btn_check_for_update_ItemClick(object sender, DevExpress.Xpf.Bars.ItemClickEventArgs e)
        {
            CheckforUpdate();
        }

        private void BarButtonItem_ItemClick(object sender, DevExpress.Xpf.Bars.ItemClickEventArgs e)
        {
            var item = (sender as BarButtonItem).DataContext as BaseLayoutItem;
            docklayout.DockController.Restore(item);
        }

        private void btn_start_startup(object sender, DevExpress.Xpf.Bars.ItemClickEventArgs e)
        {
            InstallMeOnStartUp();
        }
        void SetStartupV2(string Name,string Path)
        {
            string runKey = "SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run";
            Microsoft.Win32.RegistryKey key = Microsoft.Win32.Registry.LocalMachine.OpenSubKey(runKey, true);
            key.SetValue(Name, Path);
        }
        void InstallMeOnStartUp()
        {
            try
            {
                
                Microsoft.Win32.RegistryKey key = Microsoft.Win32.Registry.LocalMachine.OpenSubKey("SOFTWARE\\Wow6432Node\\Microsoft\\Windows\\CurrentVersion\\Run", true);
                if(key == null)
                {
                    key = Microsoft.Win32.Registry.LocalMachine.CreateSubKey("SOFTWARE\\Wow6432Node\\Microsoft\\Windows\\CurrentVersion\\Run");
                }               
                Assembly curAssembly = Assembly.GetExecutingAssembly();
                key.SetValue(curAssembly.GetName().Name, curAssembly.Location);
                SetStartupV2(curAssembly.GetName().Name, curAssembly.Location);
            }
            catch {
                App.GlobalLogger.LogError("Mainwindow","cannot set Run on startup");
            }
        }

        private void btn_show_app_setting_click(object sender, DevExpress.Xpf.Bars.ItemClickEventArgs e)
        {
            AppSettingWindow wd = new AppSettingWindow();
            wd.Owner = this;
            wd.Show();
        }

        private void btn_show_log_click(object sender, DevExpress.Xpf.Bars.ItemClickEventArgs e)
        {
            LogWindow wd = new LogWindow();
            wd.Owner = this;
            wd.Show();
        }

        private void docklayout_DockItemClosing(object sender, DevExpress.Xpf.Docking.Base.ItemCancelEventArgs e)
        {
            if (PrivilegeViewModel.Instance.CanCloseVisionView)
            {
                if (DXMessageBox.Show("Do you want to remove current vision view?", "Remove vision view", MessageBoxButton.OKCancel, MessageBoxImage.Warning) == MessageBoxResult.OK)
                {
                    MainViewModel.Instance.VisionManager.Remove(e.Item.DataContext as VisionModel);
                }
                else
                {
                    e.Cancel = true;
                    //e.Handled = true;
                }
            }
            else
            {
                e.Cancel = true;
                DXMessageBox.Show("Current user cannot close!", "User access denied", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
            
        }

        private void btn_status_Click(object sender, RoutedEventArgs e)
        {
            LogWindow wd = new LogWindow();
            wd.Owner = this;
            wd.Show();
        }

        private void MainWD_ContentRendered(object sender, EventArgs e)
        {
            MainViewModel.Instance.LoadStartupJob();
            this.ContentRendered -= MainWD_ContentRendered;
        }

        private void btn_show_tag_manager_click(object sender, DevExpress.Xpf.Bars.ItemClickEventArgs e)
        {
            GlobalTagManagerWindow wd = new GlobalTagManagerWindow();
            wd.Owner = this;
            wd.Show();
        }

        private void btn_close_remote_Click(object sender, DevExpress.Xpf.Bars.ItemClickEventArgs e)
        {
            try
            {
                ExecuteCommand("tscon.exe 1 /dest:console", 5000);
            }
            catch (Exception ex)
            {
                DXMessageBox.Show(this,ex.Message,"Error",MessageBoxButton.OK,MessageBoxImage.Error);
            }
        }
        public static int ExecuteCommand(string Command, int Timeout)
        {
            int ExitCode;
            ProcessStartInfo ProcessInfo;
            Process Process;
            ProcessInfo = new ProcessStartInfo("cmd.exe", "/C " + Command);
            ProcessInfo.CreateNoWindow = true;
            ProcessInfo.UseShellExecute = false;
            Process = Process.Start(ProcessInfo);
            Process.WaitForExit(Timeout);
            ExitCode = Process.ExitCode;
            Process.Close();
            return ExitCode;
        }
    }
    public class MVVMSerializationLayoutAdapter : ILayoutAdapter
    {
        string ILayoutAdapter.Resolve(DockLayoutManager owner, object item)
        {
            BaseLayoutItem panelHost = owner.GetItem("root");
            if (panelHost == null)
            {
                panelHost = new LayoutGroup() { Name = "root", DestroyOnClosingChildren = false };
                owner.LayoutRoot.Add(panelHost);
            }

            return "root";
        }
    }
    [JsonObject]
    public class ApplicationSetting: ReactiveObject
    {
        [JsonIgnore]
        public List<string> ThemedList { get; set; } = new List<string>()
        {
            DevExpress.Xpf.Core.Theme.DeepBlueName,
            DevExpress.Xpf.Core.Theme.VS2017BlueName,
            DevExpress.Xpf.Core.Theme.VS2017DarkName,
            DevExpress.Xpf.Core.Theme.VS2017LightName,
            DevExpress.Xpf.Core.Theme.Office2019ColorfulName,
            DevExpress.Xpf.Core.Theme.Office2019ColorfulTouchName,
            DevExpress.Xpf.Core.Theme.Office2019DarkGrayName,
            DevExpress.Xpf.Core.Theme.Office2019HighContrastName,
            DevExpress.Xpf.Core.Theme.Office2019WhiteName,
             DevExpress.Xpf.Core.Theme.Office2019BlackName,
              DevExpress.Xpf.Core.Theme.MetropolisDarkName,
               DevExpress.Xpf.Core.Theme.MetropolisLightName
        };
        string _selected_themed;
        public string SelectedThemed
        {
            get
            {
                return _selected_themed;
            }
            set
            {
                this.RaiseAndSetIfChanged(ref _selected_themed, value);
            }
        }
        bool _show_task_bar_full_screen;
        public bool ShowTaskBarFullScreen
        {
            get
            {
                return _show_task_bar_full_screen;
            }
            set
            {
                this.RaiseAndSetIfChanged(ref _show_task_bar_full_screen, value);
            }
        }

        bool _limit_day_image;
        public bool LimitDayImage
        {
            get
            {
                return _limit_day_image;
            }
            set
            {
                this.RaiseAndSetIfChanged(ref _limit_day_image, value);
            }
        }

        int _max_day_image;
        public int MaxDayImage
        {
            get
            {
                return _max_day_image;
            }
            set
            {
                this.RaiseAndSetIfChanged(ref _max_day_image, value);
            }
        }

        bool _limit_folder_image;
        public bool LimitFolderImage
        {
            get
            {
                return _limit_folder_image;
            }
            set
            {
                this.RaiseAndSetIfChanged(ref _limit_folder_image, value);
            }
        }

        int _max_folder_image;
        public int MaxFolderImage
        {
            get
            {
                return _max_folder_image;
            }
            set
            {
                this.RaiseAndSetIfChanged(ref _max_folder_image, value);
            }
        }
        int _max_temp_record_image=20;
        public int MaxTempRecordImage
        {
            get
            {
                return _max_temp_record_image;
            }
            set
            {
                this.RaiseAndSetIfChanged(ref _max_temp_record_image, value);
            }
        }

        bool _auto_start_up;
        public bool AutoStartup
        {
            get
            {
                return _auto_start_up;
            }
            set
            {
                this.RaiseAndSetIfChanged(ref _auto_start_up, value);
            }
        }
        string _startup_job = "";
        public string StartupJob
        {
            get
            {
                return _startup_job;
            }
            set
            {
                this.RaiseAndSetIfChanged(ref _startup_job, value);
            }
        }
        bool auto_online_job=false;
        public bool AutoOnlineJob
        {
            get
            {
                return auto_online_job;
            }
            set
            {
                this.RaiseAndSetIfChanged(ref auto_online_job, value);
            }
        }
        bool _SetLoadJobAsStartup = false;
        public bool SetLoadJobAsStartup
        {
            get
            {
                return _SetLoadJobAsStartup;
            }
            set
            {
                this.RaiseAndSetIfChanged(ref _SetLoadJobAsStartup, value);
            }
        }
        DisplayQuality _display_quality = DisplayQuality.Normal;
        public DisplayQuality DisplayQuality
        {
            get
            {
                return _display_quality;
            }
            set
            {
                this.RaiseAndSetIfChanged(ref _display_quality, value);
            }
        }
        string _language = "en-US";
        public string Language
        {
            get
            {
                return _language;
            }
            set
            {
                this.RaiseAndSetIfChanged(ref _language, value);
            }
        }

        public int AffinityCoreNumber { get; set; }
        bool _SetAffinityCore;
        public bool SetAffinityCore {
            get
            {
                return _SetAffinityCore;
            }
            set
            {
                this.RaiseAndSetIfChanged(ref _SetAffinityCore, value);
            }
        }
        bool _SetProcessPriority;
        public bool SetProcessPriority {
            get
            {
                return _SetProcessPriority;
            }
            set
            {
                this.RaiseAndSetIfChanged(ref _SetProcessPriority, value);
            }
        }
        string _JobDirectory = System.IO.Path.Combine(NOVisionDesigner.Workspace.WorkspaceManager.Instance.AppDataPath, "Jobs");
        public string JobDirectory
        {
            get
            {
                return _JobDirectory;
            }
            set
            {
                if (_JobDirectory != value)
                    MainViewModel.Instance.PathJobs = value;
                this.RaiseAndSetIfChanged(ref _JobDirectory, value);
            }
        }
        public System.Diagnostics.ProcessPriorityClass ProcessPriority { get; set; }
        public void Save(string FileName)
        {
            try
            {
                var data = JsonConvert.SerializeObject(this, Formatting.Indented);
                System.IO.File.WriteAllText(FileName, data);
            }catch(Exception ex)
            {
                App.GlobalLogger.LogError("Application Setting", ex.Message);
            }
            
        }
        public ApplicationSetting()
        {
            this.WhenAnyValue(x => x.SelectedThemed).Subscribe(x =>
            {
                try
                {
                    ApplicationThemeHelper.ApplicationThemeName = x;

                    ApplicationThemeHelper.SaveApplicationThemeName();
                }catch(Exception ex)
                {

                }
                
            });
        }
        public static ApplicationSetting Load(string FileName)
        {
            try
            {


                var result = JsonConvert.DeserializeObject<ApplicationSetting>(System.IO.File.ReadAllText(FileName));
                if (result.JobDirectory == null)
                    result.JobDirectory = System.IO.Path.Combine(NOVisionDesigner.Workspace.WorkspaceManager.Instance.AppDataPath, "Jobs");
                return result;
            }
            catch (Exception ex)
            {
                App.GlobalLogger.LogError("Application Setting", ex.Message);
            }
            return new ApplicationSetting();
            
        }
    }

    public static class WindowSizing
        {
            const int MONITOR_DEFAULTTONEAREST = 0x00000002;

#region DLLImports

            [DllImport("shell32", CallingConvention = CallingConvention.StdCall)]
            public static extern int SHAppBarMessage(int dwMessage, ref APPBARDATA pData);

            [DllImport("user32", SetLastError = true)]
            static extern IntPtr FindWindow(string lpClassName, string lpWindowName);

            [DllImport("user32")]
            internal static extern bool GetMonitorInfo(IntPtr hMonitor, MONITORINFO lpmi);

            [DllImport("user32")]
            internal static extern IntPtr MonitorFromWindow(IntPtr handle, int flags);

#endregion

            private static MINMAXINFO AdjustWorkingAreaForAutoHide(IntPtr monitorContainingApplication, MINMAXINFO mmi)
            {
                IntPtr hwnd = FindWindow("Shell_TrayWnd", null);
                if (hwnd == null) return mmi;
                IntPtr monitorWithTaskbarOnIt = MonitorFromWindow(hwnd, MONITOR_DEFAULTTONEAREST);
                if (!monitorContainingApplication.Equals(monitorWithTaskbarOnIt)) return mmi;
                APPBARDATA abd = new APPBARDATA();
                abd.cbSize = Marshal.SizeOf(abd);
                abd.hWnd = hwnd;
                SHAppBarMessage((int)ABMsg.ABM_GETTASKBARPOS, ref abd);
                int uEdge = GetEdge(abd.rc);
                bool autoHide = System.Convert.ToBoolean(SHAppBarMessage((int)ABMsg.ABM_GETSTATE, ref abd));

                if (!autoHide) return mmi;

                switch (uEdge)
                {
                    case (int)ABEdge.ABE_LEFT:
                        mmi.ptMaxPosition.x += 2;
                        mmi.ptMaxTrackSize.x -= 2;
                        mmi.ptMaxSize.x -= 2;
                        break;
                    case (int)ABEdge.ABE_RIGHT:
                        mmi.ptMaxSize.x -= 2;
                        mmi.ptMaxTrackSize.x -= 2;
                        break;
                    case (int)ABEdge.ABE_TOP:
                        mmi.ptMaxPosition.y += 2;
                        mmi.ptMaxTrackSize.y -= 2;
                        mmi.ptMaxSize.y -= 2;
                        break;
                    case (int)ABEdge.ABE_BOTTOM:
                        mmi.ptMaxSize.y -= 2;
                        mmi.ptMaxTrackSize.y -= 2;
                        break;
                    default:
                        return mmi;
                }
                return mmi;
            }

            private static int GetEdge(RECT rc)
            {
                int uEdge = -1;
                if (rc.top == rc.left && rc.bottom > rc.right)
                    uEdge = (int)ABEdge.ABE_LEFT;
                else if (rc.top == rc.left && rc.bottom < rc.right)
                    uEdge = (int)ABEdge.ABE_TOP;
                else if (rc.top > rc.left)
                    uEdge = (int)ABEdge.ABE_BOTTOM;
                else
                    uEdge = (int)ABEdge.ABE_RIGHT;
                return uEdge;
            }

            public static void WindowInitialized(Window window)
            {
                IntPtr handle = (new System.Windows.Interop.WindowInteropHelper(window)).Handle;
                System.Windows.Interop.HwndSource.FromHwnd(handle).AddHook(new System.Windows.Interop.HwndSourceHook(WindowProc));
            }

            private static IntPtr WindowProc(System.IntPtr hwnd, int msg, System.IntPtr wParam, System.IntPtr lParam, ref bool handled)
            {
                switch (msg)
                {
                    case 0x0024:
                        WmGetMinMaxInfo(hwnd, lParam);
                        handled = true;
                        break;
                }

                return (IntPtr)0;
            }

            private static void WmGetMinMaxInfo(IntPtr hwnd, IntPtr lParam)
            {
                MINMAXINFO mmi = (MINMAXINFO)Marshal.PtrToStructure(lParam, typeof(MINMAXINFO));
                IntPtr monitorContainingApplication = MonitorFromWindow(hwnd, MONITOR_DEFAULTTONEAREST);

                if (monitorContainingApplication != System.IntPtr.Zero)
                {
                    MONITORINFO monitorInfo = new MONITORINFO();
                    GetMonitorInfo(monitorContainingApplication, monitorInfo);
                    RECT rcWorkArea = monitorInfo.rcWork;
                    RECT rcMonitorArea = monitorInfo.rcMonitor;
                    mmi.ptMaxPosition.x = Math.Abs(rcWorkArea.left - rcMonitorArea.left);
                    mmi.ptMaxPosition.y = Math.Abs(rcWorkArea.top - rcMonitorArea.top);
                    mmi.ptMaxSize.x = Math.Abs(rcWorkArea.right - rcWorkArea.left);
                    mmi.ptMaxSize.y = Math.Abs(rcWorkArea.bottom - rcWorkArea.top);
                    mmi.ptMaxTrackSize.x = mmi.ptMaxSize.x;                                                 //maximum drag X size for the window
                    mmi.ptMaxTrackSize.y = mmi.ptMaxSize.y;                                                 //maximum drag Y size for the window
                    mmi.ptMinTrackSize.x = 800;                                                             //minimum drag X size for the window
                    mmi.ptMinTrackSize.y = 600;                                                             //minimum drag Y size for the window
                    mmi = AdjustWorkingAreaForAutoHide(monitorContainingApplication, mmi);                  //need to adjust sizing if taskbar is set to autohide
                }
                Marshal.StructureToPtr(mmi, lParam, true);
            }

            public enum ABEdge
            {
                ABE_LEFT = 0,
                ABE_TOP = 1,
                ABE_RIGHT = 2,
                ABE_BOTTOM = 3
            }

            public enum ABMsg
            {
                ABM_NEW = 0,
                ABM_REMOVE = 1,
                ABM_QUERYPOS = 2,
                ABM_SETPOS = 3,
                ABM_GETSTATE = 4,
                ABM_GETTASKBARPOS = 5,
                ABM_ACTIVATE = 6,
                ABM_GETAUTOHIDEBAR = 7,
                ABM_SETAUTOHIDEBAR = 8,
                ABM_WINDOWPOSCHANGED = 9,
                ABM_SETSTATE = 10
            }

            [StructLayout(LayoutKind.Sequential)]
            public struct APPBARDATA
            {
                public int cbSize;
                public IntPtr hWnd;
                public int uCallbackMessage;
                public int uEdge;
                public RECT rc;
                public bool lParam;
            }

            [StructLayout(LayoutKind.Sequential)]
            public struct MINMAXINFO
            {
                public POINT ptReserved;
                public POINT ptMaxSize;
                public POINT ptMaxPosition;
                public POINT ptMinTrackSize;
                public POINT ptMaxTrackSize;
            };

            [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
            public class MONITORINFO
            {
                public int cbSize = Marshal.SizeOf(typeof(MONITORINFO));
                public RECT rcMonitor = new RECT();
                public RECT rcWork = new RECT();
                public int dwFlags = 0;
            }

            [StructLayout(LayoutKind.Sequential)]
            public struct POINT
            {
                public int x;
                public int y;

                public POINT(int x, int y)
                {
                    this.x = x;
                    this.y = y;
                }
            }

            [StructLayout(LayoutKind.Sequential, Pack = 0)]
            public struct RECT
            {
                public int left;
                public int top;
                public int right;
                public int bottom;
            }
        }
    
}
