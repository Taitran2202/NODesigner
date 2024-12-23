using Newtonsoft.Json;
using NOVisionDesigner.Designer.Python;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using static NOVisionDesigner.Designer.Windows.AugmentationEditorWindow;

namespace NOVisionDesigner.Designer.Nodes
{
    public class TrainOCR: TrainPythonBase
    {
        public void TrainPython(string configDir, Action<TrainingArgs> TrainUpdate)
        {
            if (IsTrainning)
                return;
            IsTrainning = true;
            try
            {
                run_cmd("python.exe",String.Format("\"{0}\"",System.IO.Path.Combine(MainWindow.AppPath, "Designer/Python/OCR/detection/craft/train.py")) + " " + String.Format("\"{0}\"", configDir), TrainUpdate);
            }
            catch (Exception ex)
            {

            }
            IsTrainning = false;
        }
        private void run_cmd(string cmd, string args, Action<TrainingArgs> TrainUpdate)
        {

            ProcessStartInfo start = new ProcessStartInfo();
            start.FileName = cmd;//cmd is full path to python.exe
            start.Arguments = args;//args is path to .py file and any cmd line args
            start.UseShellExecute = false;
            start.RedirectStandardOutput = true;
            start.CreateNoWindow = true;
            process = new Process();
            Regex epochRegex = new Regex(@"\d+\/\d+");
            Regex accRegex = new Regex(@"reshape_accuracy: \S+");
            double accValue = 0;
            process.OutputDataReceived += (sender, e) =>
            {
                if (e.Data == null)
                    return;
                Console.WriteLine(e.Data);
                //"converted_output_accuracy: 0.09270"

                try
                {
                    if (e.Data.Contains("reshape_accuracy: "))
                    {
                        var match = accRegex.Match(e.Data);
                        if (match.Success)
                        {
                            var match1 = match.Groups[0];
                            var matchstring = match1.Value;
                            var accstring = matchstring.Substring("reshape_accuracy: ".Length);
                            if (double.TryParse(accstring, out accValue))
                            {

                            }
                        }
                    }
                }
                catch (Exception ex)
                {

                }
                if (e.Data.Contains("Epoch "))
                {
                    var result = epochRegex.Match(e.Data);
                    if (result.Success)
                    {
                        var match1 = result.Groups[0];
                        var matchstring = match1.Value;
                        var splitIndex = matchstring.IndexOf("/");
                        var epoch = matchstring.Substring(0, splitIndex);
                        var epochtotal = matchstring.Substring(splitIndex + 1);
                        int epochint;
                        int epochtotalint;
                        if (int.TryParse(epoch, out epochint))
                        {
                            if (int.TryParse(epochtotal, out epochtotalint))
                            {

                                try
                                {

                                    var TrainProgress = (double)epochint * 100 / epochtotalint;
                                    TrainUpdate?.Invoke(new TrainingArgs() { Progress = TrainProgress, State = TrainState.OnGoing, Accuracy = accValue * 100 });
                                }
                                catch (Exception ex)
                                {

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
