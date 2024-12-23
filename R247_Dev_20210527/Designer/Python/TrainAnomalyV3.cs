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
    public class TrainAnomalyV3 : TrainPythonBase
    {
        public async void TrainPython(string configDir,string model, Action<TrainingArgs> TrainUpdate)
        {
            if (IsTrainning)
                return;
            IsTrainning = true;
            try
            {
                process = NOVisionPython.CreateTrainProcess("anomaly", model, configDir);
                run_cmd(process, TrainUpdate);
            }
            catch (Exception ex)
            {
                TrainUpdate?.Invoke(new TrainingArgs() { State = TrainState.Error });
            }
            IsTrainning = false;
        }
        public void TrainConsole(string configDir,string model, Action<TrainingArgs> TrainUpdate = null)
        {
            //NOVisionPython.TrainConsole("anomaly", "anomalib", configDir);
            NOVisionPython.TrainConsole("anomaly", model, configDir, TrainUpdate);     
        }
        private void run_cmd(Process process, Action<TrainingArgs> TrainUpdate)
        {           
            process.OutputDataReceived += (sender, e) =>
            {
                TrainUpdate?.Invoke(new TrainingArgs() { State = TrainState.OnGoing, Log = e.Data });
                Console.WriteLine(e.Data);
            };
            string ErrorLog = string.Empty;
            process.ErrorDataReceived += (sender, e) =>
            {
                ErrorLog += e.Data + Environment.NewLine;
                Console.WriteLine(e.Data);
            };
            process.Start();
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();
            process.WaitForExit();
            if(process.ExitCode == 0)
            {
                TrainUpdate?.Invoke(new TrainingArgs() { State = TrainState.Completed });
            }               
            else
            {
                TrainUpdate?.Invoke(new TrainingArgs() { State = TrainState.Error,ErrorLog= ErrorLog });
            }
        }
    }
    public class TrainNOVISION : TrainPythonBase
    {
        public async void TrainPython(string type,string configDir, string model, Action<TrainingArgs> TrainUpdate)
        {
            if (IsTrainning)
                return;
            IsTrainning = true;
            try
            {
                process = NOVisionPython.CreateTrainProcess(type, model, configDir);
                run_cmd(process, TrainUpdate);
            }
            catch (Exception ex)
            {
                TrainUpdate?.Invoke(new TrainingArgs() { State = TrainState.Error });
            }
            IsTrainning = false;
        }
        public void TrainConsole(string type,string configDir, string model, Action<TrainingArgs> TrainUpdate = null)
        {
            //NOVisionPython.TrainConsole("anomaly", "anomalib", configDir);
            NOVisionPython.TrainConsole(type, model, configDir, TrainUpdate);
        }
        private void run_cmd(Process process, Action<TrainingArgs> TrainUpdate)
        {
            process.OutputDataReceived += (sender, e) =>
            {
                TrainUpdate?.Invoke(new TrainingArgs() { State = TrainState.OnGoing, Log = e.Data });
                Console.WriteLine(e.Data);
            };
            string ErrorLog = string.Empty;
            process.ErrorDataReceived += (sender, e) =>
            {
                ErrorLog += e.Data + Environment.NewLine;
                Console.WriteLine(e.Data);
            };
            process.Start();
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();
            process.WaitForExit();
            if (process.ExitCode == 0)
            {
                TrainUpdate?.Invoke(new TrainingArgs() { State = TrainState.Completed });
            }
            else
            {
                TrainUpdate?.Invoke(new TrainingArgs() { State = TrainState.Error, ErrorLog = ErrorLog });
            }
        }
    }
}
