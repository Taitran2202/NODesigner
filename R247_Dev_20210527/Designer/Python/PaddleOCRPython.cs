using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NOVisionDesigner.Designer.Python
{
    public static class PaddleOCR
    {
        public static Provider Provider = Provider.Python;
        public static string Directory = "C:/src/PaddleOCR";
        public static void TrainConsole(string type, string model, string configFile, Action<TrainingArgs> TrainUpdate = null)
        {
            if (Provider == Provider.Python)
            {

                Task.Run(() =>
                {
                    var cmdProc = System.Diagnostics.Process.Start(new ProcessStartInfo()
                    {
                        FileName = "cmd.exe",
                        Arguments = "/k python " + BuildPythonCommandArg(type, model, configFile),
                        WorkingDirectory = PaddleOCR.Directory,
                    });
                    //string output = cmdProc.StandardOutput.ReadToEnd();
                    cmdProc.WaitForExit();
                    TrainUpdate?.Invoke(new TrainingArgs() { State = TrainState.Completed });
                });

            }
            else
            {
                Task.Run(() =>
                {
                    var cmdProc = System.Diagnostics.Process.Start(new ProcessStartInfo() { 
                        FileName = "cmd.exe", 
                        Arguments = "/k NOVisionPython.exe " + BuildExeCommandArg(type, model, configFile), 
                        WorkingDirectory = PaddleOCR.Directory });
                    cmdProc.WaitForExit();
                    TrainUpdate?.Invoke(new TrainingArgs() { State = TrainState.Completed });
                });
            }

        }
        public static string BuildPythonCommandArg(string type, string model, string configFile)
        {
            string command =
                String.Format("\"{0}\"", 
                System.IO.Path.Combine(Directory, "tools","train.py"))
                + " -c " + String.Format("\"{0}\"",
                System.IO.Path.Combine(Directory, "configs", "rec","PP-OCRv3", "en_PP-OCRv3_rec.yml"))
                + " -o Global.pretrained_model=C:/paddle_pretrain/en_PP-OCRv3_rec_train/best_accuracy" ;
            return command;
        }
        public static Process CreateTrainProcess(string type, string model, string configFile)
        {
            Process process = new Process();
            if (Provider == Provider.Python)
            {
                process.StartInfo.FileName = "python.exe";
                process.StartInfo.Arguments = BuildPythonCommandArg(type, model, configFile);

            }
            else if (Provider == Provider.Exe)
            {
                //process.StartInfo.FileName = System.IO.Path.Combine(Directory, "NOVisionPython.exe");
                //process.StartInfo.Arguments = BuildExeCommandArg(type, model, configFile);
            }
            process.StartInfo.WorkingDirectory = Directory;
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.RedirectStandardOutput = true;
            process.StartInfo.RedirectStandardInput = true;
            process.StartInfo.RedirectStandardError = true;
            process.StartInfo.CreateNoWindow = true;
            return process;
        }
        public static string BuildExeCommandArg(string type, string model, string configFile)
        {
            string command = " --type=" + type + " --model=" + model + " --config=" + String.Format("\"{0}\"", configFile);
            return command;
        }
    }
}
