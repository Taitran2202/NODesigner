using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NOVisionDesigner.Windows.HelperWindows;
using ReactiveUI;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NOVisionDesigner.ViewModel
{
    public class VisionModelManager:ReactiveObject,IDisposable
    {
        public void Dispose()
        {
            foreach (var item in VisionModels)
            {
                item.Dispose();
            }
        }
        public ObservableCollection<VisionModel> VisionModels { get; set; } = new ObservableCollection<VisionModel>();
        string _base_dir;
        public string Basedir { get=>_base_dir; set=> this.RaiseAndSetIfChanged(ref _base_dir, value); }
        public string ConfigDir { get; set; }
        public VisionModelManager(string dir)
        {
            Basedir = System.IO.Path.Combine(dir, "VisionManager");
            if (!System.IO.Directory.Exists(Basedir))
            {
                System.IO.Directory.CreateDirectory(Basedir);
            }
            ConfigDir = System.IO.Path.Combine(dir, "config.json");
        }
        public void Remove(VisionModel item)
        {
            VisionModels.Remove(item);
            UserViewModel.WriteActionDatabase("VisionModelsManager", null, item.Caption, null, "Remove vision model", null);
        }
        public VisionModel AddVisionModel(string name)
        {
            var newItem = new VisionModel(Basedir, Guid.NewGuid().ToString()) { Caption = name };
            VisionModels.Add(newItem);
            UserViewModel.WriteActionDatabase("VisionModelsManager", null, null, name, "Add new vision model", null);
            return newItem;
            
        }
        public JObject Serialize()
        {

            JObject managerData = new JObject(); 
            JArray visionModels_id = new JArray();
            foreach(var item in VisionModels)
            {
                var data=item.Id;
                visionModels_id.Add(data);
            }
            managerData.Add("visionModels", visionModels_id);
            return managerData;
        }
        public void Save()
        {
            var data = Serialize();
            using (StreamWriter file = File.CreateText(ConfigDir))
            {
                JsonSerializer serializer = new JsonSerializer();
                serializer.Formatting = Formatting.Indented;
                serializer.Serialize(file, data);
            }
            foreach(var item in VisionModels)
            {
                item.Save();
            }
        }
        public void Load()
        {
            VisionModels.Clear();
            try{
                if (System.IO.File.Exists(ConfigDir))
                {
                    JObject managerData = JObject.Parse(File.ReadAllText(ConfigDir));
                    if(managerData["visionModels"]!=null)
                    {
                        foreach (var id in managerData["visionModels"])
                        {
                            VisionModel added = new VisionModel(Basedir, id.ToString());
                            VisionModels.Add(added);
                            try
                            {
                                added.Load();
                            }catch (Exception ex)
                            {
                                App.GlobalLogger.LogError("VisionModelManager","Error when loading vision model" + added.Caption);
                            }
                            

                        }
                    }
                    
                }
                
                
            }catch(Exception ex)
            {
                App.GlobalLogger.LogError("VisionModelManagers", "Error loading Vision Models: " + ex.Message);
            }
            
        } 
    }
}
