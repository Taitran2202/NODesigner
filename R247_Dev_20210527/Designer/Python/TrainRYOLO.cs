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
    public class TrainRYOLO : TrainPythonBase
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
                process = NOVisionPython.CreateTrainProcess("detection", "ryolo", configDir);
                run_cmd(process, TrainUpdate);
            }
            catch (Exception ex)
            {

            }
            IsTrainning = false;
        }

        private void run_cmd(Process process, Action<TrainingArgs> TrainUpdate)
        {
            Regex regex = new Regex("Epoch [0-9]+/ [0-9]+");
            double LossDataUpdate = 999999;
            process.OutputDataReceived += (sender, e) =>
            {
                if (e.Data == null)
                    return;
                Console.WriteLine(e.Data);
                if (e.Data.Contains("ETA"))
                {

                    int index_loss = e.Data.IndexOf("total_loss: ");
                    if (index_loss >= 0)
                    {

                        var LossData = e.Data.Substring(index_loss + 12, 6);
                        if (double.TryParse(LossData, out LossDataUpdate))
                        {

                        }


                    }
                }
                if (e.Data.Contains("Epoch "))
                {
                    var match = regex.Match(e.Data);
                    if (match.Success)
                    {

                        var result = match.Value.Substring(6);
                        var epochdata = result;
                        var index = epochdata.IndexOf('/');
                        if (index >= 0)
                        {
                            var epoch = epochdata.Substring(0, index);
                            var epochtotal = epochdata.Substring(index + 1);

                            epochint = int.Parse(epoch);
                            epochtotalint = int.Parse(epochtotal);
                            if (int.TryParse(epoch, out epochint))
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
                Console.WriteLine(e.Data);
            };
            process.Start();
            process.BeginOutputReadLine();
            process.WaitForExit();
            TrainUpdate?.Invoke(new TrainingArgs() { State = TrainState.Completed });
        }
    }
}