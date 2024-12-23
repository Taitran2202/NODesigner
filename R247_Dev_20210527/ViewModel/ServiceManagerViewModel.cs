using Newtonsoft.Json.Linq;
using NOVisionDesigner.Services;
using ReactiveUI;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using NOVisionDesigner.Services.ViewModel;
using Newtonsoft.Json;

namespace NOVisionDesigner.ViewModel
{
    public class ServiceManagerViewModel: ReactiveObject
    {
        public void Dispose()
        {
            foreach (var item in Services)
            {
                item.Dispose();
            }
        }
        public ObservableCollection<ApplicationService> Services { get; set; } = new ObservableCollection<ApplicationService>();
        public string ServiceDirectory { get; internal set; }
        public string SavePath { get; internal set; }
        public ServiceManagerViewModel()
        {
            ServiceDirectory= Workspace.WorkspaceManager.Instance.ServiceDirectory;
            SavePath = System.IO.Path.Combine(ServiceDirectory, "config.txt");
            if (System.IO.File.Exists(SavePath))
            {
                Load(SavePath);
                //run all services
                Start();
            }
        }
        void Start()
        {
            foreach(var item in Services)
            {
                item.Start();
            }
        }
        public void AddService(string type)
        {
            ApplicationService added = ServiceFactory.Create(type, ServiceDirectory,"");
            if (added != null)
            {
                Services.Add(added);
            }
        }
        public JObject Serialize()
        {

            JObject managerData = new JObject();
            JArray ServiceListData = new JArray();
            foreach (var item in Services)
            {
                JObject serviceData = new JObject();
                serviceData.Add("id", item.ID);
                serviceData.Add("type", Type.GetType(item.ToString()).Name);
                ServiceListData.Add(serviceData);
            }
            managerData.Add("ServiceList", ServiceListData);
            return managerData;
        }
        public void Save()
        {
            var data = Serialize();
            using (StreamWriter file = File.CreateText(SavePath))
            {
                JsonSerializer serializer = new JsonSerializer();
                serializer.Formatting = Formatting.Indented;
                serializer.Serialize(file, data);
            }
            foreach (var item in Services)
            {
                item.Save();
            }
        }
        void Load(string ConfigDir)
        {
            try
            {
                JObject managerData = JObject.Parse(File.ReadAllText(ConfigDir));
                if (managerData["ServiceList"] != null)
                {
                    foreach (var service in managerData["ServiceList"])
                    {
                        var id = service["id"].ToString();
                        var type = service["type"].ToString();
                        ApplicationService added = ServiceFactory.Create(type,ServiceDirectory,id);
                        if (added != null)
                        {
                            Services.Add(added);
                            try
                            {
                                added.Load();
                            }
                            catch (Exception ex)
                            {
                                App.GlobalLogger.LogError("Service manager", "Error when loading service with type: " + added.GetType().FullName);
                            }
                        }
                        


                    }
                }
            }
            catch(Exception ex)
            {

            }
            
        }
    }
    public class ServiceFactory
    {
        public static IEnumerable<Type> GetLoadableTypes(Assembly assembly)
        {
            // TODO: Argument validation
            try
            {
                return assembly.GetTypes();
            }
            catch (ReflectionTypeLoadException e)
            {
                return e.Types.Where(t => t != null);
            }
        }
        public static List<string> ServiceTypes = new List<string>();
        public static string GenID()
        {
            return Guid.NewGuid().ToString();
        }
        static ServiceFactory() {
            Assembly asm = Assembly.GetExecutingAssembly();
            var types = GetLoadableTypes(asm)
                .Where(type => (type.Namespace == "NOVisionDesigner.Services.ViewModel")).Where(x => x.BaseType != null).Where(x=>x.IsSubclassOf(typeof(NOVisionDesigner.Services.ApplicationService)));
            ServiceTypes = types.Select(type => type.Name).ToList();
        }
        public static ApplicationService Create(string type,string ServiceDir, string id)
        {
            if (id == "") { id = GenID(); }
            foreach (var item in ServiceTypes)
            {
                if (item == type)
                {
                    Type t = Type.GetType("NOVisionDesigner.Services.ViewModel." + item);
                    var created = Activator.CreateInstance(t, new object[] { ServiceDir, id });
                    if (created is ApplicationService)
                    {
                        Console.WriteLine("Created service " + type + " with ID: " + id);
                        

                        return (created as ApplicationService);
                    }
                }
            }
            return null;
        }
        
    }
}
