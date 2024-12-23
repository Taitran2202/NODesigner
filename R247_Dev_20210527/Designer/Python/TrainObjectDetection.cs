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
    public class TrainObjectDetection
    {
        public bool IsTrainning { get; set; }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="step"></param>
        /// <param name="TrainUpdate"></param>
        string GetCurrentFileName([System.Runtime.CompilerServices.CallerFilePath] string fileName = null)
        {
            return fileName;
        }
        public void TrainPython(int step, string datasetdir, string modeldir, string[] classList, Action<double, bool, bool,double> TrainUpdate)
        {
            if (IsTrainning)
                return;
            IsTrainning = true;
            try
            {
                //var listAug = JsonConvert.SerializeObject(listAugmentation);
                string currentFileName = Path.GetFileName(GetCurrentFileName());
                string currentFolder = GetCurrentFileName().Replace(currentFileName, "");
                string pre_train_model_dir = System.IO.Path.Combine(currentFolder, "ObjectDetection2", "resnet.h5");
                var classliststring = "[" + String.Join(",", classList.Select(x => string.Format("'{0}'", x)).ToArray()) + "]";
                run_cmd("python.exe", "Designer/Python/ObjectDetection2/create_object_detection_model.py " + String.Format("\"{0}\"", datasetdir)
                               + " " + String.Format("\"{0}\"", modeldir) + " "
                               + String.Format("\"{0}\"", pre_train_model_dir) + " " +

                               step.ToString() + " " +
                               string.Format("\"{0}\"", classliststring), TrainUpdate);
            }
            catch (Exception ex)
            {

            }
            IsTrainning = false;
        }
        int epochint = 0;
        int epochtotalint = 0;
        private void run_cmd(string cmd, string args, Action<double, bool, bool, double> TrainUpdate)
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
                        var epochdata = result;
                        var index = epochdata.IndexOf('/');
                        if (index >= 0)
                        {
                            var epoch = epochdata.Substring(0, index);
                            var epochtotal = epochdata.Substring(index + 1);

                            //int.TryParse(epoch, out epochint);
                            //int.TryParse(epochtotal, out epochtotalint);
                            epochint = int.Parse(epoch);
                            epochtotalint= int.Parse(epochtotal);

                        }

                    }
                }

                if (e.Data.Contains("ETA:"))
                {
                
                    int indexend = e.Data.IndexOf("[");
                    int index_loss = e.Data.IndexOf("loss");
                    if (indexend >= 0)
                    {
                        var StepData = e.Data.Substring(0, indexend - 1).Replace(" ", "");
                        var index_step = StepData.IndexOf('/');
                        var LossData = e.Data.Substring(index_loss + 5, 7).Replace(" ", "");
                        var LossDataUpdate = Convert.ToDouble(LossData);

                        if (index_step >= 0)
                        {
                            var step = StepData.Substring(0, index_step);
                            var steptotal = StepData.Substring(index_step + 1);
                            int stepint;
                            int steptotalint;
                        
                            if (int.TryParse(step, out stepint))
                            {
                                if (int.TryParse(steptotal, out steptotalint))
                                {
                                        if (epochint==0 && epochtotalint==0)
                                        {
                                            epochtotalint = 1;
                                            epochint = 1;
                                        }                               
                                        var TrainProgress = (double)((steptotalint * (epochint - 1) + stepint) * 100 / (epochtotalint * steptotalint));
                                        TrainUpdate?.Invoke(TrainProgress, false, false,LossDataUpdate);
                                    
                                  
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
            TrainUpdate?.Invoke(0, false, true,0);
        }
    }
}
