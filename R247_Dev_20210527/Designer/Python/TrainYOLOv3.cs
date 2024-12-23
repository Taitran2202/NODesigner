using Google.Apis.Auth.OAuth2;
using Google.Apis.Download;
using Google.Apis.Drive.v3;
using Google.Apis.Services;
using Google.Apis.Util.Store;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using static NOVisionDesigner.Designer.Windows.AugmentationEditorWindow;

namespace NOVisionDesigner.Designer.Python
{
    public class TrainYOLOv3:TrainPythonBase
    {
        private int epochint = 0;
        private int epochtotalint = 0;
        string GetCurrentFileName([System.Runtime.CompilerServices.CallerFilePath] string fileName = null)
        {
            return fileName;
        }
        static string[] Scopes = { DriveService.Scope.DriveReadonly };
        static string ApplicationName = "Drive API .NET Quickstart";
        public static string DownloadGoogleFile(string fileId,string downloaddir)
        {

            UserCredential credential;

            using (var stream =
                new FileStream("credentials.json", FileMode.OpenOrCreate, FileAccess.ReadWrite))
            {
                // The file token.json stores the user's access and refresh tokens, and is created
                // automatically when the authorization flow completes for the first time.
                string credPath = "token.json";
                credential = GoogleWebAuthorizationBroker.AuthorizeAsync(
                    GoogleClientSecrets.Load(stream).Secrets,
                    Scopes,
                    "user",
                    CancellationToken.None,
                    new FileDataStore(credPath, true)).Result;
                Console.WriteLine("Credential file saved to: " + credPath);
            }
            var service = new DriveService(new BaseClientService.Initializer()
            {
                HttpClientInitializer = credential,
                ApplicationName = ApplicationName,
            });
            //DriveService service = new DriveService(new BaseClientService.Initializer());
            FilesResource.GetRequest request = service.Files.Get(fileId);

            string FileName = request.Execute().Name;
            string FilePath = downloaddir;

            MemoryStream stream2 = new MemoryStream();

            request.MediaDownloader.ProgressChanged += (Google.Apis.Download.IDownloadProgress progress) =>
            {
                switch (progress.Status)
                {
                    case DownloadStatus.Downloading:
                        {
                            Console.WriteLine(progress.BytesDownloaded);
                            break;
                        }
                    case DownloadStatus.Completed:
                        {
                            Console.WriteLine("Download complete.");
                            stream2.WriteTo(new FileStream( FilePath,FileMode.OpenOrCreate)); //Save the file 
                            break;
                        }
                    case DownloadStatus.Failed:
                        {
                            Console.WriteLine("Download failed.");
                            break;
                        }
                }
            };
            request.Download(stream2);

            return FilePath;
        }
        public async void TrainPython(string configDir, Action<TrainingArgs> TrainUpdate)
        {
            if (IsTrainning)
                return;
            IsTrainning = true;
            try
            {
                //string weightsDir = Path.Combine(modeldir, "weights");
                //string savedModelDir = Path.Combine(modeldir, "saved_model");
                //string annotationPath = Path.Combine(rootdir, "annotations");
                //string imgPath = Path.Combine(rootdir, "images");
                //string currentFileName = Path.GetFileName(GetCurrentFileName());
                //string currentFolder = GetCurrentFileName().Replace(currentFileName, "");
                //string pre_train_model_path = System.IO.Path.Combine(MainWindow.AppPath,"Designer","Python", "YOLOv3", "weights", "yolov3.weights");
                //string transfer = transferType;
                //var classliststring = Path.Combine(rootdir, "annotations", "className.names.nams");
                //if (!System.IO.File.Exists(pre_train_model_path))
                //{
                //    DownloadGoogleFile("1a0s11Wvi3Y609sS0M02yxfnXmqK61d0s", pre_train_model_path);
                    
                //}
                run_cmd("python.exe", "Designer/Python/YOLOv3/train.py "

                               +String.Format("\"{0}\"", configDir), TrainUpdate);
            }
            catch (Exception ex)
            {

            }
            IsTrainning = false;
        }

        private void run_cmd(string cmd, string args, Action<TrainingArgs> TrainUpdate)
        {

            ProcessStartInfo start = new ProcessStartInfo();
            start.FileName = cmd; //cmd is full path to python.exe
            start.Arguments = args; //args is path to .py file and any cmd line args
            start.UseShellExecute = false;
            start.RedirectStandardOutput = true;
            start.CreateNoWindow = true;
            process = new Process();

            Regex regex = new Regex("=> STEP [0-9]+/[0-9]+");
            double LossDataUpdate = 999999;
            process.OutputDataReceived += (sender, e) =>
            {
                if (e.Data == null)
                    return;
                Console.WriteLine(e.Data);

                if (e.Data.Contains("=> STEP "))
                {

                    int index_loss = e.Data.IndexOf("total_loss: ");
                    if (index_loss >= 0)
                    {

                        var LossData = e.Data.Substring(index_loss + 12);
                        if(double.TryParse(LossData,out LossDataUpdate))
                        {

                        }

                        
                    }
                    var match = regex.Match(e.Data);
                    if (match.Success)
                    {

                        var result = match.Value.Substring(8);
                        var epochdata = result;
                        var index = epochdata.IndexOf('/');
                        if (index >= 0)
                        {
                            var epoch = epochdata.Substring(0, index);
                            var epochtotal = epochdata.Substring(index + 1);

                            epochint = int.Parse(epoch);
                            epochtotalint = int.Parse(epochtotal);
                            if(int.TryParse(epoch, out epochint))
                            {
                                if (int.TryParse(epochtotal, out epochtotalint))
                                {
                                    if (epochint == 0 && epochtotalint == 0)
                                    {
                                        epochtotalint = 1;
                                        epochint = 1;
                                    }
                                    var TrainProgress = (double)epochint * 100 / epochtotalint;
                                    TrainUpdate?.Invoke(new TrainingArgs() { Progress = TrainProgress, State = TrainState.OnGoing, Loss = LossDataUpdate });
                                }
                            }

                        }

                    }
                }

                
            };
            process.ErrorDataReceived += (sender, e) =>
            {
                Console.WriteLine(e);
            };
            process.StartInfo = start;
            process.Start();
            process.BeginOutputReadLine();
            process.WaitForExit();
            TrainUpdate?.Invoke(new TrainingArgs() { State = TrainState.Completed });
        }
    }
}
