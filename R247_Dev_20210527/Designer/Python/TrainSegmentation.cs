using NOVisionDesigner.Designer.Nodes;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace NOVisionDesigner.Designer.Python
{
    public class TrainSegmentation:TrainPythonBase
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="step"></param>
        /// <param name="TrainUpdate"></param>
        public void TrainPython(string configDir, Action<TrainingArgs> TrainUpdate)
        {
            if (IsTrainning)
                return;
            IsTrainning = true;
            try
            {
                process = NOVisionPython.CreateTrainProcess("segmentation", "unet", configDir);
                run_cmd(process, TrainUpdate);

            }
            catch (Exception ex)
            {
                TrainUpdate?.Invoke(new TrainingArgs() { State = TrainState.Error });
            }

            IsTrainning = false;
           
        }
        public void TrainConsole(string configDir, string model, Action<TrainingArgs> TrainUpdate = null)
        {
            //NOVisionPython.TrainConsole("anomaly", "anomalib", configDir);
            NOVisionPython.TrainConsole("segmentation", model, configDir, TrainUpdate);
        }
        private void run_cmd(Process process, Action<TrainingArgs> TrainUpdate)
        {
            Regex epochRegex = new Regex(@"\d+\/\d+");
            Regex accRegex = new Regex(@"base_model_accuracy: \S+");
            double accValue = 0;
            process.OutputDataReceived += (sender, e) =>
            {
                if (e.Data == null)
                    return;
                Console.WriteLine(e.Data);
                //"converted_output_accuracy: 0.09270"
                
                try
                {
                    if (e.Data.Contains("base_model_accuracy: "))
                    {
                        var match  = accRegex.Match(e.Data);
                        if (match.Success)
                        {
                            var match1 = match.Groups[0];
                            var matchstring = match1.Value;
                            var accstring = matchstring.Substring("base_model_accuracy: ".Length);
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
                    if(result.Success)
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
            string ErrorLog = string.Empty;
            process.ErrorDataReceived += (sender, e) =>
            {
                ErrorLog += e.Data + Environment.NewLine;
                Console.WriteLine(e.Data);
            };
            process.Start();
            process.BeginErrorReadLine();
            process.BeginOutputReadLine();
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
    public enum TrainState
    {
        Completed, Error, Cancel, OnGoing
    }
    public class TrainingArgs
    {
        public double Progress { get; set; }
        public TrainState State { get; set; }
        public double Accuracy { get; set; }
        public double Loss { get; set; }
        public TimeSpan ETA { get; set; }
        public string ErrorLog { get; set; }
        public string Log { get; set; }

    }
}
