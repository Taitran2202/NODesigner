using Newtonsoft.Json;
using ReactiveUI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NOVisionDesigner.Services
{
    [JsonObject(MemberSerialization.OptIn)]
    public class ApplicationService : ReactiveObject, IDisposable
    {
        public string ID { get; set; }
        public string BaseDir { get; set; }
        public string ConfigDir { get; set; }
        public ApplicationService()
        {

        }
        public ApplicationService(string ServiceDir,string id)
        {
            BaseDir = System.IO.Path.Combine(ServiceDir,id);
            if (!System.IO.Directory.Exists(BaseDir))
            {
                System.IO.Directory.CreateDirectory(BaseDir);
            }
            ConfigDir = System.IO.Path.Combine(BaseDir, "config.txt");
            this.ID = id;
        }
        public virtual void Start()
        {

        }
        public virtual void Stop()
        {

        }
        public virtual void Save()
        {

        }
        public virtual void Load()
        {

        }
        ServiceState _state= ServiceState.Stopped;
        public ServiceState State
        {
            get
            {
                return _state;
            }
            set
            {
                this.RaiseAndSetIfChanged(ref _state, value);
            }
        }
        bool _is_enabled;
        [JsonProperty]
        public bool IsEnabled
        {
            get
            {
                return _is_enabled;
            }
            set
            {
                this.RaiseAndSetIfChanged(ref _is_enabled, value);
            }
        }
        public virtual void Dispose()
        {

        }
    }

    public enum ServiceState
    {
        Running=0, Stopped=1, Initializing=2, Error=3
    }
}
