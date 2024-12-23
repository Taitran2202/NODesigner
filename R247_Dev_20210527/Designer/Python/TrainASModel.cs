using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using static NOVisionDesigner.Designer.Windows.AugmentationEditorWindow;

namespace NOVisionDesigner.Designer.Python
{
    public class TrainASModel:TrainPythonBase
    {
        public void TrainPython(string configDir, Action<TrainingArgs> TrainUpdate, Action<string> log = null)
        {
            if (IsTrainning)
                return;
            IsTrainning = true;
            try
            {
                log?.Invoke("python.exe " + "Designer/Python/AnomalySegmentationDetection/train.py " + " "
                               + String.Format("\"{0}\"", configDir));
                run_cmd("python.exe", "Designer/Python/AnomalySegmentationDetection/train.py " + " "
                               + String.Format("\"{0}\"", configDir) , TrainUpdate,log);
            }
            catch (Exception ex)
            {

            }
            IsTrainning = false;
        }

        private void run_cmd(string cmd, string args, Action<TrainingArgs> TrainUpdate,Action<string> log=null)
        {

            ProcessStartInfo start = new ProcessStartInfo();
            start.FileName = cmd;//cmd is full path to python.exe
            start.Arguments = args;//args is path to .py file and any cmd line args
            start.UseShellExecute = false;
            start.RedirectStandardOutput = true;
            start.CreateNoWindow = true;
            process = new Process();
            Regex regex = new Regex("Epoch [0-9]+/[0-9]+");
            double loss = 0;
            double step_sec = 0;
            Regex regexLoss = new Regex(@" loss: \b\d[\d,.]*\b");
            Regex regexEta = new Regex(@"\d+s");
            process.OutputDataReceived += (sender, e) =>
            {
                if (e.Data == null)
                    return;
                Console.WriteLine(e.Data);
                var matchloss = regexLoss.Match(e.Data);
                if (matchloss.Success)
                {
                    var result = matchloss.Value.Substring(7);
                    if (double.TryParse(result, out loss))
                    {

                    }
                }
                var matcheta = regexEta.Match(e.Data);
                if (matcheta.Success)
                {
                    var result = matcheta.Value.Substring(0,matcheta.Value.IndexOf("s"));
                    if (double.TryParse(result, out step_sec))
                    {

                    }
                }
                if (e.Data.Contains("Epoch "))
                {
                    var match = regex.Match(e.Data);
                    if (match.Success)
                    {


                        var result = match.Value.Substring(5);

                        //int indexend = e.Data.IndexOf("[");

                        var epochdata = result;
                        var index = epochdata.IndexOf('/');
                        if (index >= 0)
                        {
                            var epoch = epochdata.Substring(0, index);
                            var epochtotal = epochdata.Substring(index + 1);
                            int epochint;
                            int epochtotalint;
                            if (int.TryParse(epoch, out epochint))
                            {
                                if (int.TryParse(epochtotal, out epochtotalint))
                                {
                                    try
                                    {
                                        var TrainProgress = (double)epochint * 100 / epochtotalint;

                                        TrainUpdate?.Invoke(new TrainingArgs()
                                        {
                                            State = TrainState.OnGoing,
                                            Progress = TrainProgress,
                                            Loss = loss,
                                            ETA =
                                            TimeSpan.FromSeconds((epochtotalint - epochint) * step_sec)
                                        }) ;
                                    }
                                    catch (Exception ex)
                                    {

                                    }
                                }
                            }
                        }

                    }
                }
            };
            if (log != null)
            {
                process.OutputDataReceived += (sender, e) =>
                {
                    log(e.Data);
                };
            }
            process.ErrorDataReceived += (sender, e) =>
            {
                Console.WriteLine(e);
            };
            process.StartInfo = start;
            process.Start();
            process.BeginOutputReadLine();
            process.WaitForExit();
            TrainUpdate?.Invoke(new TrainingArgs() {State= TrainState.Completed,Progress=0, Accuracy=0});
        }
    }
}
