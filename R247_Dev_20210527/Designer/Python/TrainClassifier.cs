using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using static NOVisionDesigner.Designer.Windows.AugmentationEditorWindow;

namespace NOVisionDesigner.Designer.Python
{
    public class TrainClassifier
    {
        public bool IsTrainning { get; set; }
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
                process = NOVisionPython.CreateTrainProcess("classification", "v1", configDir);
                //run_cmd("python.exe", "Designer/Python/create_a_new_model_classifier.py " + configDir, TrainUpdate);
                run_cmd(process, TrainUpdate);
            }
            catch (Exception ex)
            {

            }
            IsTrainning = false;
        }
        public string GetTrainCommand(string configDir)
        {
            return "Designer/Python/create_a_new_model_classifier.py " + configDir;
        }
        public void Cancel()
        {
            if (IsTrainning)
            {
                if (process != null)
                {
                    process.Kill();
                    process = null;
                }
            }
        }
        Process process;
        public class TrainUpdateContext
        {
            public double Progress { get; set; }
            public double Loss { get; set; }
            public double Acc { get; set; }
        }
        private void run_cmd(Process process, Action<TrainingArgs> TrainUpdate)
        {
            this.process = process;
            Regex regex = new Regex("Epoch [0-9]+/[0-9]+");
            Regex regexAcc = new Regex(@"accuracy: \b\d[\d,.]*\b");
            double acc = 0;
            process.OutputDataReceived += (sender, e) =>
            {
                if (e.Data == null)
                    return;
                Console.WriteLine(e.Data);
                if(e.Data.Contains("accuracy: "))
                {
                    var matchacc = regexAcc.Match(e.Data);
                    if (matchacc.Success)
                    {
                        var result = matchacc.Value.Substring(10);
                        if (double.TryParse(result, out acc))
                        {

                        }
                    }
                }
                
                if (e.Data.Contains("Epoch "))
                {
                    double TrainProgress = 0;
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
                                        TrainProgress = (double)epochint * 100 / epochtotalint;
                                        TrainUpdate?.Invoke(new TrainingArgs() { Progress = TrainProgress, State = TrainState.OnGoing, Accuracy = acc });

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
            process.ErrorDataReceived += (sender, e) =>
            {
                Console.WriteLine(e);
            };
            process.Start();
            process.BeginOutputReadLine();
            process.WaitForExit();
            process = null;
            TrainUpdate?.Invoke(new TrainingArgs() { State = TrainState.Completed });
        }
    }
    public class TrainPythonBase
    {
        public bool IsTrainning { get; set; }
        public Process process;
        public void Cancel()
        {
            if (IsTrainning)
            {
                if (process != null)
                {
                    try
                    {
                        process.Kill();
                    }catch(Exception ex)
                    {

                    }
                    
                    process = null;
                }
            }
        }
    }
}
