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
    public class TrainRYOLOv4:TrainPythonBase
    {
        private int epochint = 0;
        private int epochtotalint = 0;
      
        public async void TrainPython(string configDir, Action<TrainingArgs> TrainUpdate)
        {
            if (IsTrainning)
                return;
            IsTrainning = true;
            try
            {
                process = NOVisionPython.CreateTrainProcess("detection", "ryolov4", configDir);
                run_cmd(process, TrainUpdate);
            }
            catch (Exception ex)
            {

            }
            IsTrainning = false;
        }

        private void run_cmd(Process process, Action<TrainingArgs> TrainUpdate)
        {
            process.OutputDataReceived += (sender, e) =>
            {
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
