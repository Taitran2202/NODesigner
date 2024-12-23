using DevExpress.Xpf.Core;
using DynamicData;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Linq;
using NodeNetwork.ViewModels;
using NOVisionDesigner.Data;
using NOVisionDesigner.Designer.Misc;
using NOVisionDesigner.UserControls;
using NOVisionDesigner.Windows;
using NOVisionDesigner.Windows.HelperWindows;
using ReactiveUI;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms.Integration;
using System.Windows.Input;
using System.Windows.Media;

namespace NOVisionDesigner.ViewModel
{
    public class BaseClassConverter : CustomCreationConverter<Tag>
    {
        private TagType _currentObjectType;

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            var jobj = JObject.ReadFrom(reader);
            _currentObjectType = jobj["Type"].ToObject<TagType>();
            return base.ReadJson(jobj.CreateReader(), objectType, existingValue, serializer);
        }

        public override Tag Create(Type objectType)
        {
            switch (_currentObjectType)
            {
                case TagType.Int:
                    return new IntTag();
                case TagType.Float:
                    return new FloatTag();
                case TagType.String:
                    return new StringTag();
                case TagType.Bool:
                    return new BoolTag();
                default:
                    throw new NotImplementedException();
            }
        }
    }
    public enum TagType
    {
        Int,
        String,
        Float,
        Bool
    }

    public abstract class Tag
    {
        public string Name { get; set; }
        public TagType Type { get; set; }
        public abstract object Value { get; set; }
    }

    public class IntTag : Tag
    {
        public int IntValue { get; set; }
        public override object Value
        {
            get { return IntValue; }
            set 
            { 
                try
                {
                    IntValue  = Convert.ToInt32(value);
                    // The conversion was successful
                }
                catch (FormatException)
                {
                    
                } 
            }
        }

        public IntTag()
        {
            Type = TagType.Int;
        }
    }

    public class StringTag : Tag
    {
        public string StringValue { get; set; }
        public override object Value
        {
            get { return StringValue; }
            set { StringValue = Convert.ToString(value); }
        }

        public StringTag()
        {
            Type = TagType.String;
        }
    }

    public class FloatTag : Tag
    {
        public float FloatValue { get; set; }
        public override object Value
        {
            get { return FloatValue; }
            set { FloatValue = (float)Convert.ToDouble(value); }
        }

        public FloatTag()
        {
            Type = TagType.Float;
        }
    }

    public class BoolTag : Tag
    {
        public bool BoolValue { get; set; }
        public override object Value
        {
            get { return BoolValue; }
            set { BoolValue = Convert.ToBoolean(value); }
        }

        public BoolTag()
        {
            Type = TagType.Bool;
        }
    }

    public class GlobalTagManager
    {
        private static GlobalTagManager _instance;

        public static GlobalTagManager Instance
        {
            get 
            {
                if (_instance == null)
                {
                    _instance = new GlobalTagManager();
                }

                return _instance;
                
            
            }
        }

        private readonly ObservableCollection<Tag> _tags;
        public ObservableCollection<Tag> Tags
        {
            get { return _tags; }
        }
        private GlobalTagManager()
        {
            _tags = new ObservableCollection<Tag>();
        }

        public void AddTag(Tag tag)
        {
            if (!(tag is IntTag || tag is StringTag || tag is FloatTag || tag is BoolTag))
            {
                throw new Exception("Invalid tag type");
            }

            _tags.Add(tag);
        }

        public Tag GetTag(string name)
        {
            var tag = _tags.FirstOrDefault(t => t.Name == name);

            //if (tag == null)
            //{
            //    throw new Exception("Tag not found");
            //}

            return tag;
        }
        public void SetValue(string TagName,object TagValue)
        {
            var tag = GetTag(TagName);
            if (tag != null)
            {
                tag.Value = TagValue;
            }

        }
        public object GetValue(string TagName)
        {
            var tag = GetTag(TagName);
            if (tag != null)
            {
                return tag.Value;
            }
            else
            {
                return null;
            }

        }
        public void UpdateTag(Tag tag)
        {
            var existingTag = GetTag(tag.Name);
            var index = _tags.IndexOf(existingTag);
            _tags[index] = tag;
        }

        public void Save()
        {
            var filePath = System.IO.Path.Combine(Workspace.WorkspaceManager.Instance.AppDataPath, "GlobalTagManagerConfig.txt");
                try
                {
                    var json = JsonConvert.SerializeObject(_tags,Formatting.Indented, new JsonSerializerSettings
                    {
                        TypeNameHandling = TypeNameHandling.All
                    });
                    File.WriteAllText(filePath, json);
                }
                catch (Exception ex)
                {
                    App.GlobalLogger.LogError("GlobalTagManager", "save error: " + ex.Message);
                }

           
            
        }

        public void Load()
        {
            var filePath = System.IO.Path.Combine(Workspace.WorkspaceManager.Instance.AppDataPath, "GlobalTagManagerConfig.txt");
            if (System.IO.File.Exists(filePath))
            {
                try
                {
                    var json = File.ReadAllText(filePath);
                    _tags.Clear();
                    _tags.AddRange(JsonConvert.DeserializeObject<ObservableCollection<Tag>>(json, new JsonSerializerSettings
                    {
                        TypeNameHandling = TypeNameHandling.All
                    }));
                }
                catch (Exception ex)
                {
                    App.GlobalLogger.LogError("GlobalTagManager", "Load error: " + ex.Message);
                }
                
            }

        }
    }
}
