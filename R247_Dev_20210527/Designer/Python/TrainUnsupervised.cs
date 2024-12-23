using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace NOVisionDesigner.Designer.Nodes
{
    class TrainUnsupervised
    {
        public bool IsTrainning { get; set; }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="step"></param>
        /// <param name="TrainUpdate"></param>
        public void TrainPython(int step, string listAugmentation, string imagedir, string modeldir, int extractor_layers, Action<double, bool, bool> TrainUpdate)
        {
            if (IsTrainning)
                return;
            IsTrainning = true;
            try
            {
                run_cmd("python.exe", "Designer/Python/anomalyTrain.py " + String.Format("\"{0}\"", imagedir)
                    + " " + String.Format("\"{0}\"", extractor_layers.ToString()) + " "
                    + " " + String.Format("\"{0}\"", modeldir) + " "
                    + " 512 512 "
                    + step.ToString(), TrainUpdate);    
            }
            catch (Exception ex)
            {

            }
            IsTrainning = false;
            }
        private void run_cmd(string cmd, string args, Action<double, bool, bool> TrainUpdate)
        {

            ProcessStartInfo start = new ProcessStartInfo();
            start.FileName = cmd;//cmd is full path to python.exe
            start.Arguments = args;//args is path to .py file and any cmd line args
            start.UseShellExecute = false;
            start.RedirectStandardOutput = true;
            start.CreateNoWindow = true;
            Process process = new Process();
            Regex regex = new Regex("Epoch [0-9]+/[0-9]+");
            process.OutputDataReceived += (sender, e) =>
            {
                if (e.Data == null)
                    return;
                Console.WriteLine(e.Data);
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
                                        TrainUpdate?.Invoke(TrainProgress, false, false);
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
            process.StartInfo = start;
            process.Start();
            process.BeginOutputReadLine();
            process.WaitForExit();
            TrainUpdate?.Invoke(0, false, true);
        }
    }
}
