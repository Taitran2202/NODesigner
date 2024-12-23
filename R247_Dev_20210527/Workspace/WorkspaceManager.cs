using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NOVisionDesigner.Workspace
{
    public class WorkspaceManager
    {
        private static WorkspaceManager _instance;
        public static WorkspaceManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new WorkspaceManager();
                }
                return _instance;
            }
        }
        public string jobPath;
        public string jobDataPath;
        public string AppDataPath;
        public string RecordPath;
        public string ServiceDirectory;
        public string GenJobPath(string jobname)
        {
            return System.IO.Path.Combine(jobPath, jobname);
        }
        private WorkspaceManager()
        {
            AppDataPath = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "R247");
            //AppDataPath = @"C:\Users\AVN\AppData\Roaming\webinspectionData";
            if (!Directory.Exists(AppDataPath))
            {
                Directory.CreateDirectory(AppDataPath);
            }
            jobPath = System.IO.Path.Combine(AppDataPath, "Jobs");
            if (!Directory.Exists(jobPath))
            {
                Directory.CreateDirectory(jobPath);
            }
            jobDataPath = System.IO.Path.Combine(AppDataPath, "JobData");
            if (!Directory.Exists(jobPath))
            {
                Directory.CreateDirectory(jobDataPath);
            }
            RecordPath = System.IO.Path.Combine(AppDataPath, "Record");
            if (!Directory.Exists(RecordPath))
            {
                Directory.CreateDirectory(RecordPath);
            }

            ServiceDirectory = System.IO.Path.Combine(AppDataPath, "Services");
            if (!Directory.Exists(ServiceDirectory))
            {
                Directory.CreateDirectory(ServiceDirectory);
            }
        }
    }
}
