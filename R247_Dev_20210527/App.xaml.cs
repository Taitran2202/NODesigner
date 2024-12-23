using DevExpress.Xpf.Core;
using NodeNetwork;
using NOVisionDesigner.Data;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Threading;
using HalconDotNet;
using System.Reflection;
using System.Windows.Media;
using NOVision.Windows;
using System.Diagnostics;

namespace NOVisionDesigner
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        protected override void OnExit(ExitEventArgs e)
        {
            //save app setting

            base.OnExit(e);
        }
        private static MessageBoxResult ShowThreadExceptionDialog(string title, Exception e)
        {
            string errorMsg = "An application error occurred. Please contact the adminstrator " +
                "with the following information:\n\n";
            errorMsg = errorMsg + e.Message + "\n\nStack Trace:\n" + e.StackTrace;
            return System.Windows.MessageBox.Show(errorMsg, title, MessageBoxButton.YesNoCancel,
                MessageBoxImage.Stop);
        }
        private  void Form1_UIThreadException(object sender, ThreadExceptionEventArgs t)
        {
            MessageBoxResult result = MessageBoxResult.Cancel;
            try
            {
                result = ShowThreadExceptionDialog("Windows Forms Error", t.Exception);
            }
            catch
            {
                try
                {
                    System.Windows.MessageBox.Show("Fatal Windows Forms Error",
                        "Fatal Windows Forms Error", MessageBoxButton.YesNoCancel, MessageBoxImage.Stop);
                }
                finally
                {
                    //this.Shutdown();
                }
            }

            // Exits the program when the user clicks Abort.
            if (result == MessageBoxResult.Cancel)
            {
                //this.Shutdown();
            }

        }
        private void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            try
            {
                Exception ex = (Exception)e.ExceptionObject;
                string errorMsg = "An application error occurred. Please contact the adminstrator " +
                    "with the following information:\n\n";

                // Since we can't prevent the app from terminating, log this to the event log.
                if (!EventLog.SourceExists("ThreadException"))
                {
                    EventLog.CreateEventSource("ThreadException", "Application");
                }

                // Create an EventLog instance and assign its source.
                EventLog myLog = new EventLog();
                myLog.Source = "ThreadException";
                myLog.WriteEntry(errorMsg + ex.Message + "\n\nStack Trace:\n" + ex.StackTrace);
            }
            catch (Exception exc)
            {
                try
                {
                    System.Windows.MessageBox.Show("Fatal Non-UI Error",
                        "Fatal Non-UI Error. Could not write the error to the event log. Reason: "
                        + exc.Message, MessageBoxButton.OK, MessageBoxImage.Stop);
                }
                finally
                {
                    //this.Shutdown();
                }
            }
        }
        void CatchWinformException()
        {
            System.Windows.Forms.Application.ThreadException += new ThreadExceptionEventHandler(Form1_UIThreadException);

            // Set the unhandled exception mode to force all Windows Forms errors to go through
            // our handler.
            System.Windows.Forms.Application.SetUnhandledExceptionMode(System.Windows.Forms.UnhandledExceptionMode.CatchException);

            // Add the event handler for handling non-UI thread exceptions to the event.
            AppDomain.CurrentDomain.UnhandledException +=
                new UnhandledExceptionEventHandler(CurrentDomain_UnhandledException);
        }
        protected override void OnStartup(StartupEventArgs e)
        {

            AppSettingPath = System.IO.Path.Combine(NOVisionDesigner.MainWindow.AppPath, "ApplicationSetting.ini");
            RenderOptions.ProcessRenderMode = System.Windows.Interop.RenderMode.SoftwareOnly;
            try
            {
                ApplicationThemeHelper.UpdateApplicationThemeName();
            }catch(Exception ex)
            {

            }
            

            Thread SplashThread = new Thread(ShowSplash);
            SplashThread.SetApartmentState(ApartmentState.STA);
            SplashThread.IsBackground = true;
            SplashThread.Name = "Splash Screen";
            SplashThread.Start();

            created.WaitOne();
            bool is_shutdown = false;
            App.splash.UpdateStatus("LOADING IMAGE PROCCESSING LIBRARY");
            try
            {
                HOperatorSet.SetSystem("clip_region", "false");
                App.splash.UpdateStatus("LOADING IMAGE PROCCESSING LIBRARY PASSED");


            }
            catch (Exception lib_ex)
            {
                // MessageBox.Show(this, "Halcon Licence has Expired!!");
                //App.splash.Close();
                App.splash.ShowMessageBox(lib_ex.Message);
                App.splash.CloseSplash();
                is_shutdown = true;
                //  this.Shutdown();

                //return;
            }

            Environment.SetEnvironmentVariable("TF_FORCE_GPU_ALLOW_GROWTH", "true");
            //DevExpress.Xpf.Core.ApplicationThemeHelper.ApplicationThemeName = DevExpress.Xpf.Core.Theme.VS2017BlueName;
            //ApplicationThemeHelper.UpdateApplicationThemeName();
            
            base.OnStartup(e);
            NNViewRegistrar.RegisterSplat();


            if (is_shutdown)
            {
                Shutdown(1);
                return;
            }
            else
            {

                this.StartupUri = new Uri("MainWindow.xaml", UriKind.Relative);
                this.ShutdownMode = System.Windows.ShutdownMode.OnMainWindowClose;
            }
            try
            {
                LoadSetting();
            }catch(Exception ex)
            {

            }
            

            //Handle winform exceptions
            CatchWinformException();



            //AppDomain.CurrentDomain.AssemblyResolve += CurrentDomain_AssemblyResolve;
            //NOVisionDesigner.Designer.Encryption.AES.EncryptFile(@"C:\Users\Nam\Downloads\ch_ppocr_server_v2.0_rec_infer\ch_ppocr_server_v2.0_rec_infer\model.onnx",
            //    @"C:\Users\Nam\Downloads\ch_ppocr_server_v2.0_rec_infer\ch_ppocr_server_v2.0_rec_infer\model_en.onnx", "something");
        }
        public static void LoadSetting()
        {
            
            AppSetting = ApplicationSetting.Load(AppSettingPath);
            NOVisionDesigner.Workspace.WorkspaceManager.Instance.jobPath = AppSetting.JobDirectory;
            if (AppSetting.SetProcessPriority)
            {
                var currentProcess = System.Diagnostics.Process.GetCurrentProcess();
                currentProcess.PriorityClass = AppSetting.ProcessPriority;
            }
            if (AppSetting.SetAffinityCore)
            {
                var currentProcess = System.Diagnostics.Process.GetCurrentProcess();
                long AffinityMask = (long)currentProcess.ProcessorAffinity;
                var core_count = Environment.ProcessorCount;
                if (AppSetting.AffinityCoreNumber >= core_count)
                {
                    //long mask = 0;
                    //for (int i = 0; i < core_count; i++)
                    //{
                    //    mask |= ((long)1 << (core_count - i - 1));
                    //}
                    //AffinityMask &=mask;
                }
                else
                {
                    long mask = 0;
                    for (int i = 0; i < AppSetting.AffinityCoreNumber; i++)
                    {
                        mask |= ((long)1 << (core_count- i-1));
                    }
                    AffinityMask &= mask;
                }
                //AffinityMask &= 0x0FC0;
                currentProcess.ProcessorAffinity = (IntPtr)AffinityMask;
            }
            if (AppSetting.LimitDayImage)
            {
                RecordHelper.CleanDirectory(AppSetting.MaxDayImage);
            }

        }
        public static void SaveSetting()
        {
            AppSetting.Save(AppSettingPath);
            ViewModel.MainViewModel.Instance.ServiceManager.Save();
        }
        private Assembly CurrentDomain_AssemblyResolve(object sender, ResolveEventArgs args)
        {
            var domain = (AppDomain)sender;

            foreach (var assembly in domain.GetAssemblies())
            {
                if (assembly.FullName == args.Name)
                {
                    return assembly;
                }
            }

            return null;
        }
        ManualResetEvent created = new ManualResetEvent(false);
        public static SplashScreen.SplashScreen1 splash;
        public void ShowSplash()
        {
            splash = new SplashScreen.SplashScreen1();
            splash.Show();

            created.Set();
            System.Windows.Threading.Dispatcher.Run();
        }
        public static string AppSettingPath;
        public static ApplicationSetting AppSetting = new ApplicationSetting();
        public static LogProvider GlobalLogger = new LogProvider();
        public static RecordFolderHelper RecordHelper = new RecordFolderHelper();
    }
}
