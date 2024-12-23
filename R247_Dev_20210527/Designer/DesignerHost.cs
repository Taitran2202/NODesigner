using DynamicData;
using HalconDotNet;
using Newtonsoft.Json.Linq;
using NodeNetwork.Toolkit.BreadcrumbBar;
using NodeNetwork.Toolkit.Group;
using Newtonsoft.Json;
using NodeNetwork.Toolkit.NodeList;
using NodeNetwork.Toolkit.ValueNode;
using NodeNetwork.ViewModels;
using NOVisionDesigner.Designer.GroupNodes;
using NOVisionDesigner.Designer.Misc;
using NOVisionDesigner.Designer.Nodes;
using NOVisionDesigner.Helper;
using NOVisionDesigner.ViewModel;
using NOVisionDesigner.Windows;
using NOVisionDesigner.Windows.HelperWindows;
using NOVisionDesigner.Workspace;
using ReactiveUI;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Reactive.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using static NOVisionDesigner.Designer.Misc.InspectionContext;
using NOVisionDesigner.Designer.SimpleView;
using NOVisionDesigner.Designer.Communication;
using System.Windows.Media.Imaging;

namespace NOVisionDesigner.Designer
{
    [JsonObject]
    public class TagManagerModel : ReactiveObject, INotifyPropertyChanged
    {
        public string Name { get; set; }
        public ObservableCollection<Tag> TagList { get; set; } = new ObservableCollection<Tag>();
        public TagManagerModel()
        {

        }
        public static JObject Save(TagManagerModel tagmanager)
        {
            return (JObject)JToken.FromObject(tagmanager);
        }
        public static TagManagerModel Load(JObject data)
        {
            return data.ToObject<TagManagerModel>();
        }
        public void SendTag(string tagname)
        {
            TagList.FirstOrDefault(x => x.Name == tagname).Value++;
        }
        public void Reset()
        {
            foreach(var item in TagList)
            {
                item.Value = 0;
            }
        }
        public void AddTag(string tagName)
        {
            TagList.Add(new Tag() { Name = tagName });
        }
        public void RemoveTag(string tagName)
        {
            var selected = TagList.FirstOrDefault(x => x.Name == tagName);
            TagList.Remove(selected);
        }
        public void RemoveTag(Tag tag)
        {
            TagList.Remove(tag);
        }
    }
    public class Tag : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
        public void RaisePropertyChanged(string prop)
        {
            if (PropertyChanged != null) { PropertyChanged(this, new PropertyChangedEventArgs(prop)); }
        }
        string _name;
        public string Name
        {
            get { return _name; }
            set
            {
                if (_name != value)
                {
                    _name = value;
                    RaisePropertyChanged("Name");
                }
            }
        }
        [JsonIgnore]
        double _value;
        [JsonIgnore]
        public double Value
        {
            get { return _value; }
            set
            {
                if (_value != value)
                {
                    _value = value;
                    RaisePropertyChanged("Value");
                }
            }
        }
    }
    class NetworkBreadcrumb : BreadcrumbViewModel
    {
        #region Network
        private NetworkViewModel _network;
        public NetworkViewModel Network
        {
            get => _network;
            set => this.RaiseAndSetIfChanged(ref _network, value);
        }
        #endregion
    }
    [JsonObject]
    public class HMI: ReactiveObject
    {
        private ObservableCollection<NodeBinding> _BindingList = new ObservableCollection<NodeBinding>();
        public ObservableCollection<NodeBinding> BindingList
        {
            get { return _BindingList; }
            set { this.RaiseAndSetIfChanged(ref _BindingList, value); }
        }
        public bool AlwaysShowMenu { get; set; } = true;
        public string Title { get; set; } = "Menu";
        public double Width { get; set; }
        public double Height { get; set; }
        public double PosX { get; set; }
        public double PosY { get; set; }
        double _menu_opacity=1;
        public double MenuOpacity
        {
            get
            {
                return _menu_opacity;
            }
            set
            {
                this.RaiseAndSetIfChanged(ref _menu_opacity, value);
            }
        }
        int _font_size=14;
        public int FontSize {
            get
            {
                return _font_size;
            } 
            set {
                this.RaiseAndSetIfChanged(ref _font_size, value);
            }
        }
        bool _is_edit;
        [JsonIgnore]
        public bool IsEdit
        {
            get
            {
                return _is_edit;
            }
            set
            {
                this.RaiseAndSetIfChanged(ref _is_edit, value);
            }
        }
       
        public void Initialize(DesignerHost host)
        {
            foreach (var item in BindingList)
            {
                var binding = item;
                if (binding != null)
                {
                    if (binding.NodeID != null)
                    {
                        var node = host.Network.Nodes.Items.First(x => x.ID == binding.NodeID);
                        if (node != null)
                        {
                            binding.Node = node;
                            binding.NodeID = node.ID;
                        }
                    }
                    else
                    {
                        if(host.TagManager.TagList.Any(x => x.Name == binding.Label))
                        {
                            var tag = host.TagManager.TagList.First(x => x.Name == binding.Label);
                            binding.TagIndex = host.TagManager.TagList.IndexOf(tag);
                            binding.TagManager = host.TagManager;
                        }
                        
                    }
                }

            }
            foreach (var tag in host.TagManager.TagList)
            {
                
                if (!BindingList.Any(x=>x.Label==tag.Name))
                {
                    var binding = new NodeBinding() { Label = tag.Name, TagManager = host.TagManager, TagIndex = host.TagManager.TagList.IndexOf(tag) };
                    BindingList.Add(binding);
                }
            }
            
        }
        public HMI()
        {

        }
        public DesignerHost host;      
        public HMI(DesignerHost host)
        {
            this.host = host;
        }
        public static JObject Save(HMI hmi)
        {
            return (JObject)JToken.FromObject(hmi);
        }
        public static HMI Load(JObject data,DesignerHost host)
        {
            var result =  data.ToObject<HMI>();
            result.host = host;
            return result;
        }
    }

    public class ProcessStatistic
    {
        public double ProcessingTime { get; set; }
        public DateTime RecordTime { get; set; }
    }
    public class AddNodeInfo
    {
        public string TypeName { get; set; }
        public string TypeNode { get; set; }
        public string DisplayName { get; set; }
        public string Description { get; set; }
        public string Icon { get; set; }
        public int SortIndex { get; set; }
        public Type NodeType { get; set; }
    }
    public class DesignerHost : ReactiveObject, IDisposable
    {
        public bool UseDefaultLayout = false;
        public List<HMITag> GetTagList()
        {
            List<HMITag> tagList = new List<HMITag>();
            foreach(var item in Network.Nodes.Items)
            {
                if (item != null & item is BaseNode)
                {
                    var proplist = item.GetType().GetProperties().Where(x => x.IsDefined(typeof(HMIProperty), false)).Select(x => x.Name);
                    foreach(var prop in proplist)
                    {
                        tagList.Add(new HMITag()
                        {
                            address = item.Name + "." + prop,
                            name = prop,
                            type = 2
                        }) ;
                    }
                    var extendlist = (item as BaseNode).GetExtendTag();
                    foreach (var prop in extendlist)
                    {
                        tagList.Add(new HMITag()
                        {
                            address = item.Name + "." + prop,
                            name = prop,
                            type = 2
                        });
                    }

                }
            }
            return tagList;
        }

        private HMI _hmi = new HMI();
        public HMI HMI
        {
            get
            {
                return _hmi;
            }
            set
            {

                this.RaiseAndSetIfChanged(ref _hmi, value);
            }
        }
        public TagManagerModel TagManager = new TagManagerModel();
        private bool _result = true;
        public bool Result
        {
            get
            {
                return _result;
            }
            set
            {

                this.RaiseAndSetIfChanged(ref _result, value);
            }
        }
        private Recorder _recorder = new Recorder();
        public Recorder recorder
        {
            get
            {
                return _recorder;
            }
            set
            {

                this.RaiseAndSetIfChanged(ref _recorder, value);
            }
        }
        private double _processing_time = 0;
        public double ProcessingTime
        {
            get
            {
                return _processing_time;
            }
            set
            {

                this.RaiseAndSetIfChanged(ref _processing_time, value);
            }
        }
        public EventHandler OnComplete;
        public Calibration Calibration { get; set; } = new Calibration();

        NetworkViewModel _network = new NetworkViewModel();
        public NetworkViewModel Network
        {
            get
            {
                return _network;
            }
            set
            {
                this.RaiseAndSetIfChanged(ref _network, value);
            }
        }
        

        private Statistics _statistics = new Statistics();
        public Statistics ListStatistics
        {
            get { return _statistics; }
            set { this.RaiseAndSetIfChanged(ref _statistics, value); }
        }
        public string NameCamera
        {
            get { var name = "Camera " + (indexCamera + 1).ToString();
                return name;
            }
        }
        //public void CreateSimpleView()
        //{
        //    if(BindingList!=null)
        //    {
        //        foreach(var item in BindingList)
        //        {
        //            var selectednode = Network.Nodes.Items.First(x => x.Name == item.NodeName);
        //            if (selectednode != null)
        //            {
        //                item.CreateEditor(selectednode);
        //            }
                    
        //        }
        //    }
        //}
        public BreadcrumbBarViewModel NetworkBreadcrumbBar { get; } = new BreadcrumbBarViewModel();
        public List<NodeViewModel> CopySelectedNodes { get; set; } = new List<NodeViewModel>();
        public string BaseDir { get; set; }
        public static List<string> nodeTypes;
        public List<AddNodeFactory> AddNodeFunction = new List<AddNodeFactory>();
        public static List<AddNodeInfo> ListAddNodeInfo = new List<AddNodeInfo>();
        public int indexCamera { get; set; }
        public void OnLoadComplete()
        {
            foreach(var item in Network.Nodes.Items)
            {
                (item as BaseNode)?.OnLoadComplete();
            }
        }

        public OnCompleted EventOnCompleted { get; set; }

        public NodeGrouper grouper;
        public void Save(bool SaveNodeData=true)
        {

            string filepath = ConfigDir;
            string tempfilePath = ConfigDir + ".tmp";
            try
            {
                using (StreamWriter file = File.CreateText(tempfilePath))
                {
                    JsonSerializer serializer = new JsonSerializer();
                    serializer.Formatting = Formatting.Indented;
                    serializer.Serialize(file, Serialize(SaveNodeData));
                }
                if (System.IO.File.Exists(filepath))
                {
                    System.IO.File.Copy(filepath, filepath + ".backup", true);
                }
                
                System.IO.File.Copy(tempfilePath, filepath, true);
            }
            catch(Exception ex)
            {

            }
            
        }
        public void ResetNodePosition()
        {
            var count = Network.Nodes.Items.Count();
            int row_num = Math.Max(1,(int)Math.Sqrt(count));
            int col_num = row_num;
            int distance = 400;
            int i = 0;
            int j = 0;
            foreach (var item in Network.Nodes.Items)
            {
                
                item.Position = new Point(j * distance, distance *i);
                j = j + 1;
                if (j > col_num)
                {
                    i = i + 1;
                    j = 0;
                }
            }
   
        }
        public void CleanUnusedNodes()
        {
            var Nodedirectories = Directory.GetDirectories(BaseDir);
            foreach (var directory in Nodedirectories)
            {
                var directoryName = new DirectoryInfo(directory).Name;
                if (Network.Nodes.Items.Count(x => x.ID == directoryName) == 0)
                {
                    System.IO.Directory.Delete(directory,true);
                    continue;
                }
            }
        }
        public void Load()
        {
            if (System.IO.File.Exists(ConfigDir))
            {
                using (StreamReader r = new StreamReader(ConfigDir))
                {
                    var fileData = r.ReadToEnd();
                    if (fileData != "")
                    {
                        var objData = JObject.Parse(fileData);
                        JObject designer_data = objData;
                        JArray nodes = designer_data["nodes"] as JArray;
                        foreach (var node in nodes)
                        {
                            string id = node["ID"].ToString();
                            string type = node["type"].ToString();
                            double x = Double.Parse(node["x"].ToString());
                            double y = Double.Parse(node["y"].ToString());
                            BaseNode new_node = AddNode(type, id) as BaseNode;

                            if (new_node != null)
                            {
                                new_node.Position = new System.Windows.Point(x, y);
                                if (File.Exists(Path.Combine(BaseDir, id, "config.bin")))
                                {
                                    string file_config = Path.Combine(BaseDir, id, "config.bin");
                                    try
                                    {
                                        DeserializeFactory item = new DeserializeFactory(new HFile(file_config, "input_binary"), new HSerializedItem(), file_config);
                                        new_node.Load(item);
                                    }

                                    catch { continue; }
                                }
                            }
                            
                        }
                        JArray connections = designer_data["connections"] as JArray;
                        foreach (var connection in connections)
                        {
                            string inputID = connection["inputID"].ToString();
                            string outputID = connection["outputID"].ToString();
                            string outputPortType = connection["outputPortType"].ToString();
                            string inputPortType = connection["inputPortType"].ToString();
                            //new version check
                            BaseNode input_node = FindNodeByID(inputID) as BaseNode;
                            BaseNode output_node = FindNodeByID(outputID) as BaseNode;
                            if (connection["outputPortName"]!=null)
                            {
                                string outputPortName = connection["outputPortName"].ToString();
                                string inputPortName = connection["inputPortName"].ToString();
                                var inputPort = input_node.Inputs.Items.Where(port => port.Name == inputPortName).FirstOrDefault();
                                var outputPort = output_node.Outputs.Items.Where(port => port.Name == outputPortName).FirstOrDefault();
                                if(inputPort!=null & outputPort != null)
                                {
                                    Network.Connections.Add(new ConnectionViewModel(Network, inputPort, outputPort));
                                }
                                
                            }
                            else
                            {
                                if (input_node != null)
                                {
                                    Network.Connections.Add(new ConnectionViewModel(Network, input_node.Inputs.Items.Where(port => port.PortType == inputPortType).First(), output_node.Outputs.Items.Where(port => port.PortType == outputPortType).First()));
                                }
                                

                            }
                            
                        }
                        JArray groupData = designer_data["groupData"] as JArray;
                        LoadGroupData(groupData);

                        
                        JObject TagManagerData = designer_data["TagManager"] as JObject;
                        if (TagManagerData != null)
                        {
                            TagManager = TagManagerModel.Load(TagManagerData);
                        }
                        JObject HMIData = designer_data["HMIData"] as JObject;
                        if (HMIData != null)
                        {
                            HMI = HMI.Load(HMIData, this);
                            HMI.Initialize(this);
                        }
                        JToken displayDataJson = designer_data["displayData"];
                        if (displayDataJson != null)
                        {
                            try
                            {
                                var displayData = displayDataJson.ToObject<DisplaySetting>();
                                //this.displayData = displayData;
                                this.displayData.ShowLastFail = displayData.ShowLastFail;
                                this.displayData.ShowLiveImage = displayData.ShowLiveImage;
                                this.displayData.Layout = displayData.Layout;
                                this.displayData.ImagePart = displayData.ImagePart;
                                this.displayData.Width = displayData.Width;
                                this.displayData.Height = displayData.Height;
                                this.displayData.PosX = displayData.PosX;
                                this.displayData.PosY = displayData.PosY;
                            }
                            catch (Exception ex)
                            {

                            }
                            
                            
                        }

                    }

                }
                OnLoadComplete();
            }
        }
        private DisplaySetting _displayData = new DisplaySetting();
        public DisplaySetting displayData
        {
            get
            {
                return _displayData;
            }
            set
            {

                this.RaiseAndSetIfChanged(ref _displayData, value);
            }
        }
        //public DisplaySetting displayData = new DisplaySetting();
        public JObject Serialize(bool SaveNodeData=true)
        {
            var TagManagerData = TagManagerModel.Save(TagManager);
            var HMIData = HMI.Save(HMI);
            var displayData = SerializeDisplaySetting();
            JArray groupData = SaveGroupData();
            JArray nodes = new JArray();
            JArray connections = new JArray();
            foreach (var c in Network.Connections.Items)
            {
                JObject connection = new JObject();
                connection.Add("inputID", (c.Input.Parent as BaseNode).ID);
                connection.Add("outputID", (c.Output.Parent as BaseNode).ID);
                connection.Add("inputPortType", c.Input.PortType);
                connection.Add("outputPortType", c.Output.PortType);
                connection.Add("inputPortName", c.Input.Name);
                connection.Add("outputPortName", c.Output.Name);
                connections.Add(connection);
            }
            foreach (var n in Network.Nodes.Items)
            {
                JObject node = new JObject();
                node.Add("ID", (n as BaseNode).ID);
                node.Add("type", Type.GetType(n.ToString()).Name);
                node.Add("x", n.Position.X.ToString());
                node.Add("y", n.Position.Y.ToString());
                nodes.Add(node);
                if (SaveNodeData)
                {
                    string pat = System.IO.Path.Combine(BaseDir, (n as BaseNode).ID, "config_temp.bin");
                    HFile file_config = new HFile(pat, "output_binary");
                    try
                    {
                        (n as BaseNode).Save(file_config);
                        file_config.Dispose();
                        System.IO.File.Copy(pat, System.IO.Path.Combine(BaseDir, (n as BaseNode).ID, "config.bin"), true);

                    }
                    catch (Exception ex)
                    {

                    }
                }
            }
            JObject designer_data = new JObject();
            designer_data.Add("nodes", nodes);
            designer_data.Add("connections", connections);
            designer_data.Add("groupData", groupData);
            designer_data.Add("HMIData", HMIData);
            designer_data.Add("displayData", displayData.Result);
            designer_data.Add("TagManager", TagManagerData);
            LoadGroupData(groupData);
            return designer_data;

        }
        public async  Task<JToken> SerializeDisplaySetting()
        {
            JToken data = null;
            await Application.Current.Dispatcher.BeginInvoke(new Action(() =>
            {
                displayData.ImagePart = displayMainWindow.HImagePart;
                data = JToken.FromObject(displayData);
            }));
            return data;
        }
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
        static DesignerHost()
        {
            Assembly asm = Assembly.GetExecutingAssembly();
            var types = GetLoadableTypes(asm)
                .Where(type => (type.Namespace == "NOVisionDesigner.Designer.Nodes")).Where(x => x.BaseType != null).Where(x => x.BaseType.FullName == "NOVisionDesigner.Designer.BaseNode");
            nodeTypes = types.Select(type => type.Name).ToList();
            foreach (var item in nodeTypes)
            {

                Type t = Type.GetType("NOVisionDesigner.Designer.Nodes." + item);
                string typename = null;
                var prop = t.GetProperty("Type");
                if (prop != null)
                {
                    var value = prop.GetValue(null) as string;
                    if (value != null)
                    {
                        typename = value;
                    }
                }
                if (typename == null)
                {
                    typename = item;
                }

                string typeNode = null;
                string displayname = "";
                string description = "";
                string icon = "";
                int sortIndex = 0;
                NodeInfo nodeInfo = (NodeInfo)Attribute.GetCustomAttribute(t, typeof(NodeInfo));
                if (nodeInfo != null)
                {
                    typeNode = nodeInfo.TypeNode;
                    description = nodeInfo.Description;
                    displayname = nodeInfo.DisplayName;
                    icon = nodeInfo.Icon;
                    sortIndex = nodeInfo.SortIndex;
                }
                if (nodeInfo.Visible)
                {
                    ListAddNodeInfo.Add(new AddNodeInfo()
                    {
                        TypeName = typename,
                        TypeNode = typeNode,
                        Description = description,
                        DisplayName = displayname,
                        SortIndex = sortIndex,
                        Icon= icon,
                        NodeType = t
                    });
                }
               
                
                
            }
        }
        private void Initialize()
        {
            //Environment.SetEnvironmentVariable("TF_GPU_ALLOCATOR", "cuda_malloc_async", EnvironmentVariableTarget.Process);
            HOperatorSet.SetSystem("clip_region","false");
            HOperatorSet.SetSystem("graphic_stack_size", 300);
            foreach(var item in ListAddNodeInfo)
            {
                AddNodeFunction.Add(new AddNodeFactory(item.TypeName, 
                    item.TypeNode, item.Icon,item.SortIndex, () => Activator.CreateInstance(item.NodeType, 
                    new object[] { this, BaseDir, GenID() }) as NodeViewModel) { 
                    DisplayName = item.DisplayName, Description = item.Description 
                });

            }



        }
        public void Dispose()
        {
            foreach(var node in this.Network.Nodes.Items)
            {
                (node as BaseNode).Dispose();
            }
            this.Network.Nodes.Dispose();
            //Network.Nodes.Clear();
        }
        public HSmartWindowControlWPF display;
        public HSmartWindowControlWPF displayMainWindow;
        public HSmartWindowControlWPF displayMainWindowFail;
        public void SetDisplay(HSmartWindowControlWPF display)
        {
            this.display = display;
            //this.displayCamera = displayCamera;
        }
        public void SetDisplayMainWindow(HSmartWindowControlWPF display, HSmartWindowControlWPF display_fail)
        {
            this.displayMainWindow = display;
            this.displayMainWindow.HImagePart = displayData.ImagePart;
            this.displayMainWindowFail = display_fail;
            //this.displayCamera = displayCamera;
        }
        public void OpenGroupNode()
        {
            try
            {
                var selectedGroupNode = Network.SelectedNodes.Items.First() as GroupNode;
                if (selectedGroupNode != null)
                {
                    NetworkBreadcrumbBar.ActivePath.Add(new NetworkBreadcrumb
                    {
                        Network = selectedGroupNode.Subnet,
                        Name = selectedGroupNode.Name
                    });
                }
                
            }
            catch { }
        }
        public void CopyGroupNode()
        {
            if(Network.SelectedNodes.Items.Count() == 0) { return; }
            CopySelectedNodes = Network.SelectedNodes?.Items?.ToList();
            CopySelectedNodes.Sort((x, y) =>
            {
                return x.Position.X.CompareTo(y.Position.X);
            });
            (Application.Current.MainWindow.DataContext as MainViewModel).CopySelectedNodes = CopySelectedNodes;
            (Application.Current.MainWindow.DataContext as MainViewModel).CopyDir = BaseDir;
            (Application.Current.MainWindow.DataContext as MainViewModel).CopyConnections = Network.Connections;
        }
        public void PasteGroupNode(Point pt)
        {
            MainViewModel mvm = (Application.Current.MainWindow.DataContext as MainViewModel);
            ISourceList<ConnectionViewModel> copyConnections;
            List<NodeViewModel> selectedNodes = new List<NodeViewModel>();
            if (CopySelectedNodes?.Count == 0) 
            { 
                if(mvm.CopySelectedNodes?.Count() == 0){ return; }
                else 
                {
                    selectedNodes = mvm.CopySelectedNodes;
                    copyConnections = mvm.CopyConnections;
                    foreach (var node in selectedNodes)
                    {
                        if(node.GetType() == typeof(GroupNode))
                        {
                            foreach (var n in (node as GroupNode).IOBinding.SubNetwork.Nodes.Items)
                            {
                                CopyNodeFolder(n);
                            }
                            continue;
                        }
                        if(node.GetType() == typeof(SegmentationNode))
                        {
                            CopySegmentFolder(node);
                        }
                        else
                        {
                            CopyNodeFolder(node);
                        }
                        
                        
                    }
                }
            }
            else 
            { 
                selectedNodes = CopySelectedNodes;
                copyConnections = Network.Connections;
            }
           // var selectedNodes = CopySelectedNodes;
            Point firstNodePos = new Point(0,0);
            Point offsetPos = new Point(0, 0);
            List<string> listOldID = new List<string>();
            List<string> listNewID = new List<string>();
            List<string> listGroupOldID = new List<string>();
            List<string> listGroupNewID = new List<string>();
            List<GroupNode> listGroupNode = new List<GroupNode>();
            List<NodeViewModel> groupSubNodeList = new List<NodeViewModel>();
            JArray groupData = new JArray();
            foreach (var node in selectedNodes)
            {
                if (node.GetType() == typeof(GroupNode))
                {
                    listGroupNode.Add(node as GroupNode);
                }
            }
            #region Copy Group
            if (listGroupNode.Count >0)
            {
                foreach (var group in listGroupNode)
                {
                    JObject subNodeData = new JObject();
                    List<string> listOldGroupNodeID = new List<string>();
                    List<string> listNewGroupNodeID = new List<string>();
                    string oldGroupID = group.ID;
                    string newGroupID = GenID();
                    string newEntraceID = GenID();
                    string newExitID = GenID();

                    #region IOGroupData
                    /*
                     * Data for Create/Copy IO of GroupNode and Create connection of GroupNode with other Node
                     */
                    JArray IOGroupData = new JArray();
                    foreach (var port in group.Inputs.Items)
                    {
                        foreach (var c in port.Connections.Items)
                        {
                            JObject IOPortData = new JObject();
                            IOPortData.Add("inputID", newEntraceID);
                            IOPortData.Add("outputID", (c.Output.Parent as BaseNode).ID);
                            IOPortData.Add("inputPortName", c.Input.Name);
                            IOPortData.Add("outputPortName", c.Output.Name);
                            IOPortData.Add("Type", "EntranceNode");
                            IOPortData.Add("Index", group.Inputs.Items.IndexOf(port).ToString());
                            IOGroupData.Add(IOPortData);
                        }
                    }
                    foreach (var port in group.Outputs.Items)
                    {
                        foreach (var c in port.Connections.Items)
                        {
                            JObject IOPortData = new JObject();
                            IOPortData.Add("inputID", (c.Input.Parent as BaseNode).ID);
                            IOPortData.Add("outputID", newExitID);
                            IOPortData.Add("inputPortName", c.Input.Name);
                            IOPortData.Add("outputPortName", c.Output.Name);
                            IOPortData.Add("Type", "ExitNode");
                            IOPortData.Add("Index", group.Outputs.Items.IndexOf(port).ToString());
                            IOGroupData.Add(IOPortData);
                        }
                    }
                    #endregion
                    #region nodeDatas for position
                    /*
                     * Data for Create/Copy Node inside Group and set position
                     */
                    JArray nodeDatas = new JArray();
                    foreach (var node in group.IOBinding.SubNetwork.Nodes.Items) //position
                    {
                        JObject nodeData = new JObject();

                        if (node.GetType() != typeof(GroupSubnetIONodeViewModel))
                        {
                            string newNodeID = GenID();
                            string oldNodeID = (node as BaseNode).ID;
                            nodeData.Add("ID", newNodeID);
                            listOldGroupNodeID.Add(oldNodeID);
                            listNewGroupNodeID.Add(newNodeID);
                            nodeData.Add("Type", "BaseNode");
                        }
                        else
                        {
                            nodeData.Add("Type", "GroupNode");
                            if ((node as GroupSubnetIONodeViewModel)._isEntranceNode)
                            {
                                string newNodeID = newEntraceID;
                                string oldNodeID = (node as BaseNode).ID;
                                nodeData.Add("ID", newNodeID);
                                listOldGroupNodeID.Add(oldNodeID);
                                listNewGroupNodeID.Add(newNodeID);
                                nodeData.Add("isEntranceNode", "true");
                            }
                            else
                            {
                                string newNodeID = newExitID;
                                string oldNodeID = (node as BaseNode).ID;
                                nodeData.Add("ID", newNodeID);
                                listOldGroupNodeID.Add(oldNodeID);
                                listNewGroupNodeID.Add(newNodeID);
                                nodeData.Add("isEntranceNode", "false");
                            }
                        }
                        nodeData.Add("x", node.Position.X.ToString());
                        nodeData.Add("y", node.Position.Y.ToString());
                        nodeDatas.Add(nodeData);
                    }
                    #endregion
                    #region subnetConnection
                    /*
                     * Data for Create/Copy IO of GroupSubnetIONode (Input / Output Node inside GroupNode) and Create connection of Node inside GroupNode
                     */
                    JArray subnetConnection = new JArray();
                    foreach (var output in group.IOBinding.EntranceNode.Outputs.Items)
                    {
                        foreach (var c in output.Connections.Items)
                        {
                            JObject subnetData = new JObject();
                            string oldNodeID = (c.Input.Parent as BaseNode).ID;
                            string newNodeID = listNewGroupNodeID.ElementAt(listOldGroupNodeID.IndexOf(oldNodeID));
                            subnetData.Add("inputID", newNodeID);
                            subnetData.Add("outputID", newEntraceID);
                            subnetData.Add("inputPortName", c.Input.Name);
                            subnetData.Add("outputPortNam", c.Output.Name);
                            subnetData.Add("Type", "EntranceNode");
                            subnetData.Add("Index", group.IOBinding.EntranceNode.Outputs.Items.IndexOf(output).ToString());
                            subnetConnection.Add(subnetData);
                        }
                    }
                    foreach (var input in group.IOBinding.ExitNode.Inputs.Items)
                    {
                        foreach (var c in input.Connections.Items)
                        {
                            JObject subnetData = new JObject();
                            string oldNodeID = (c.Output.Parent as BaseNode).ID;
                            string newNodeID = listNewGroupNodeID.ElementAt(listOldGroupNodeID.IndexOf(oldNodeID));
                            subnetData.Add("inputID", newExitID);
                            subnetData.Add("outputID", newNodeID);
                            subnetData.Add("inputPortName", c.Input.PortType);
                            subnetData.Add("outputPortName", c.Output.PortType);
                            subnetData.Add("Type", "ExitNode");
                            subnetData.Add("Index", group.IOBinding.ExitNode.Inputs.Items.IndexOf(input).ToString());
                            subnetConnection.Add(subnetData);
                        }
                    }
                    #endregion
                    subNodeData.Add("subnetConnection", subnetConnection);
                    subNodeData.Add("IOGroupData", IOGroupData);
                    subNodeData.Add("nodeDatas", nodeDatas);
                    subNodeData.Add("GroupID", newGroupID);
                    groupData.Add(subNodeData);
                    listOldID.Add(group.ID);
                    listNewID.Add(newGroupID);
                    #region Create New Copy Node
                    foreach (var node in group.IOBinding.SubNetwork.Nodes.Items) //position
                    {
                        if (node.GetType() != typeof(GroupSubnetIONodeViewModel))
                        {                            
                            string oldNodeID = (node as BaseNode).ID;
                            string ID = listNewGroupNodeID.ElementAt(listOldGroupNodeID.IndexOf(oldNodeID));
                            var type = Type.GetType(node.ToString()).Name;
                            var x = pt.X;
                            var y = pt.Y;
                            BaseNode newNode = AddNode(type, ID) as BaseNode;
                            if (firstNodePos.Y == 0)
                            {
                                firstNodePos = new Point(node.Position.X, node.Position.Y);
                            }
                            else
                            {
                                offsetPos.X = firstNodePos.X - node.Position.X;
                                offsetPos.Y = firstNodePos.Y - node.Position.Y;
                            }
                            newNode.Position = new Point(pt.X - offsetPos.X, pt.Y - offsetPos.Y);
                            string pat = System.IO.Path.Combine(BaseDir, (node as BaseNode).ID, "temp_config.bin");
                            HFile temp_config = new HFile(pat, "output_binary");
                            (node as BaseNode).Save(temp_config);
                            temp_config.Dispose();
                            if (File.Exists(Path.Combine(BaseDir, (node as BaseNode).ID, "temp_config.bin")))
                            {
                                string file_config = Path.Combine(BaseDir, (node as BaseNode).ID, "temp_config.bin");
                                try
                                {
                                    DeserializeFactory item = new DeserializeFactory(new HFile(file_config, "input_binary"), new HSerializedItem(), file_config);
                                    newNode.Load(item);
                                    newNode.ID = ID;
                                }
                                catch { continue; }
                            }
                            listGroupOldID.Add((node as BaseNode).ID);
                            listGroupNewID.Add(ID);
                            node.IsSelected = false;
                            newNode.IsSelected = true;
                        }
                    }
                    #endregion

                }
            }
            #endregion
            #region Create New Node
            foreach (var node in selectedNodes)
            {
                if (node.GetType() == typeof(GroupNode))
                {
                    continue;
                }
                var ID = GenID();
                var type = Type.GetType(node.ToString()).Name;
                var x = pt.X;
                var y = pt.Y;
                BaseNode newNode = AddNode(type, ID) as BaseNode;
                if (firstNodePos.Y == 0)
                {
                    firstNodePos = new Point(node.Position.X, node.Position.Y);
                }
                else
                {
                    offsetPos.X = firstNodePos.X - node.Position.X;
                    offsetPos.Y = firstNodePos.Y - node.Position.Y;
                }
                newNode.Position = new Point(pt.X - offsetPos.X, pt.Y - offsetPos.Y);
                string pat = System.IO.Path.Combine(BaseDir, (node as BaseNode).ID, "temp_config.bin");
                HFile temp_config = new HFile(pat, "output_binary");
                (node as BaseNode).Save(temp_config);
                temp_config.Dispose();
                if (File.Exists(Path.Combine(BaseDir, (node as BaseNode).ID, "temp_config.bin")))
                {
                    string file_config = Path.Combine(BaseDir, (node as BaseNode).ID, "temp_config.bin");
                    try
                    {
                        DeserializeFactory item = new DeserializeFactory(new HFile(file_config, "input_binary"), new HSerializedItem(), file_config);
                        newNode.Load(item);
                        newNode.ID = ID;
                        if (newNode.GetType() == typeof(SegmentationNode))
                        {
                            CopySegmentFolder(newNode,node);
                        }
                    }
                    catch { continue; }
                }
                listOldID.Add((node as BaseNode).ID);
                listNewID.Add(ID);
                //node.IsSelected = false;
                //newNode.IsSelected = true;
            }
            #endregion
            #region old
            //foreach (var node in groupSubNodeList)
            //{
            //    var ID = GenID();
            //    var type = Type.GetType(node.ToString()).Name;
            //    var x = pt.X;
            //    var y = pt.Y;
            //    BaseNode newNode = AddNode(type, ID) as BaseNode;
            //    if (firstNodePos.Y == 0)
            //    {
            //        firstNodePos = new Point(node.Position.X, node.Position.Y);
            //    }
            //    else
            //    {
            //        offsetPos.X = firstNodePos.X - node.Position.X;
            //        offsetPos.Y = firstNodePos.Y - node.Position.Y;
            //    }
            //    newNode.Position = new Point(pt.X - offsetPos.X, pt.Y - offsetPos.Y);
            //    string pat = System.IO.Path.Combine(BaseDir, (node as BaseNode).ID, "temp_config.bin");
            //    HFile temp_config = new HFile(pat, "output_binary");
            //    (node as BaseNode).Save(temp_config);
            //    temp_config.Dispose();
            //    if (File.Exists(Path.Combine(BaseDir, (node as BaseNode).ID, "temp_config.bin")))
            //    {
            //        string file_config = Path.Combine(BaseDir, (node as BaseNode).ID, "temp_config.bin");
            //        try
            //        {
            //            DeserializeFactory item = new DeserializeFactory(new HFile(file_config, "input_binary"), new HSerializedItem(), file_config);
            //            newNode.Load(item);
            //            newNode.ID = ID;
            //        }
            //        catch { continue; }
            //    }
            //    listOldID.Add((node as BaseNode).ID);
            //    listNewID.Add(ID);
            //    node.IsSelected = false;
            //    newNode.IsSelected = true;
            //}

            #endregion


            if (listGroupNode.Count > 0) //Group New node into GroupNode
            {
                LoadGroupData(groupData, listGroupNewID, listGroupOldID);
            }

            #region Create Connection
            for (int i = 0; i< listNewID.Count; i++)
            {
                string oldID = listOldID[i];
                string newID = listNewID[i];
                var oldNode = selectedNodes.Where(node => (node as BaseNode).ID == oldID).First();

                if (oldNode.GetType() == typeof(GroupNode))
                {
                    continue;
                }
                #region Create Node Input Connection
                //Check if Output of Node have Conenction.
                bool isOutputConnected = copyConnections.Items.Select(item => (item.Output.Parent as BaseNode).ID).Where(item => item == (oldNode as BaseNode).ID).Any();
                if (isOutputConnected)
                {
                    var listOutput = copyConnections.Items.Where(item => (item.Output.Parent as BaseNode).ID == (oldNode as BaseNode).ID).Select(item => item);
                    var _outputPortType = listOutput.Select(item => item.Output.PortType).First();
                    BaseNode outputNode = FindNodeByID(newID) as BaseNode;
                    for (int j = 0; j< listOldID.Count;j++)
                    {
                        bool isTwoNodeConnected = listOutput.Select(item => (item.Input.Parent as BaseNode).ID == listOldID[j]).FirstOrDefault(item => item);
                        if (isTwoNodeConnected)
                        {
                            BaseNode inputNode = FindNodeByID(listNewID[j]) as BaseNode;
                            var _inputPortType = listOutput.Where(item => (item.Input.Parent as BaseNode).ID == listOldID[j]).Select(item => item.Output.PortType).First();
                            var inputPort = inputNode.Inputs.Items.Where(port => port.PortType == _inputPortType).First();
                            if (inputPort.Connections.Items.Count() > 0) { Network.Connections.Remove(inputPort.Connections.Items.First()); }                   
                            Network.Connections.Add(new ConnectionViewModel(Network, inputPort,
                                outputNode.Outputs.Items.Where(port => port.PortType == _outputPortType).First()));                  
                        }
                        //listOutput = listOutput.Where(item => (item.Input.Parent as BaseNode).ID == _oldID);
                    }
                }
                #endregion
                #region Create Node Output Connection
                BaseNode input_node = FindNodeByID(newID) as BaseNode;
                var listPortType = input_node.Inputs.Items.ToList();
                var listOldInputPortType = oldNode.Inputs.Items.ToList();
                for (int j = 0; j < listPortType.Count; j++)
                {
                    bool isOldInputConnected = listOldInputPortType[j].Connections.Count == 0;
                    if (isOldInputConnected) { continue; }
                    bool isInputConnected = listPortType[j].Connections.Count > 0;
                    if (isInputConnected) { continue; }

                    var outputID = copyConnections.Items.Where(item => (item.Input.Parent as BaseNode).ID == oldID)
                        .Where(item => item.Output.Port.Parent.PortType == listPortType[j].PortType)
                        .Select(item => (item.Output.Parent as BaseNode).ID).FirstOrDefault();
                    // var outputPortType = Network.Connections.Items.Where(item => (item.Input.Parent as BaseNode).ID == oldID).Select(item => item.Output.Name).First();
                    ///BaseNode output_node = FindNodeByID(outputID) as BaseNode;
                    ///
                    if(outputID == null) { continue; }
                    //var findOutputNode = Network.Nodes.Items.Where(node => (node as BaseNode).ID == outputID);
                    //if(findOutputNode.Count() == 0) { continue; }
                    BaseNode output_node = Network.Nodes.Items.Where(node => (node as BaseNode).ID == outputID).FirstOrDefault() as BaseNode;
                    if(output_node == null) { 
                        output_node = selectedNodes.Where(node => (node as BaseNode).ID == outputID).FirstOrDefault() as BaseNode;
                        if(output_node == null) { continue; }
                    }
                    if (output_node.GetType() == typeof(GroupNode))
                    {
                       // if(listOldID.IndexOf(outputID) == -1) { }
                        var newOutputID = listOldID.IndexOf(outputID) == -1? outputID: listNewID.ElementAt(listOldID.IndexOf(outputID));
                        BaseNode newOutputNode = FindNodeByID(newOutputID) as BaseNode;
                        var inputPort = input_node.Inputs.Items.Where(port => port.PortType == listPortType[j].PortType).First();
                        var myIndex = -1;
                        foreach(var c in output_node.Outputs.Items)
                        {
                            if(myIndex != -1){ break; }
                            foreach(var port in c.Connections.Items)
                            {
                                if (myIndex != -1) { break; }
                                if ((port.Input.Parent as BaseNode).ID == oldID)
                                {
                                    myIndex = output_node.Outputs.Items.IndexOf(c);
                                }
                            }
                        }
                        Network.Connections.Add(new ConnectionViewModel(Network, input_node.Inputs.Items.Where(port => port.PortType == listPortType[j].PortType).First(),
                            newOutputNode.Outputs.Items.Where(port => port.PortType == listPortType[j].PortType).ElementAt(myIndex)));
                        continue;
                    }
                    Network.Connections.Add(new ConnectionViewModel(Network, input_node.Inputs.Items.Where(port => port.PortType == listPortType[j].PortType).First(), 
                        output_node.Outputs.Items.Where(port => port.PortType == listPortType[j].PortType).First()));
                }
                #endregion
                #region Select new node
                Network.ClearSelection();
                foreach(var id in listNewID)
                {
                    BaseNode node = FindNodeByID(id) as BaseNode;
                    node.IsSelected = true;
                }
                #endregion
                #region old
                //var listInputPortType = Network.Connections.Items.Where(item => (item.Input.Parent as BaseNode).ID == oldID).Select(item => item.Input.Name).ToList();
                //for (int j = 0; j < listInputPortType.Count; j++)
                //{
                //    bool isInputConnected = Network.Connections.Items.Select(item => item.Input).Where(item => item.Name == listInputPortType[j]).Where(item => item.)
                //}
                //bool isInputConnected = Network.Connections.Items.Select(item => (item.Input.Parent as BaseNode).ID).Where(item => item == (oldNode as BaseNode).ID).Any();

                //var outputID = Network.Connections.Items.Where(item => (item.Input.Parent as BaseNode).ID == oldID).Select(item => (item.Output.Parent as BaseNode).ID).FirstOrDefault();
                //var inputPortType = Network.Connections.Items.Where(item => (item.Input.Parent as BaseNode).ID == oldID).Select(item => item.Input.Name).First();
                //var outputPortType = Network.Connections.Items.Where(item => (item.Input.Parent as BaseNode).ID == oldID).Select(item => item.Output.Name).First();
                //BaseNode input_node = FindNodeByID(newID) as BaseNode;
                //BaseNode output_node = FindNodeByID(outputID) as BaseNode;
                //Network.Connections.Add(new ConnectionViewModel(Network, input_node.Inputs.Items.Where(port => port.Name == inputPortType).First(), output_node.Outputs.Items.Where(port => port.Name == outputPortType).First()));
                #endregion
            }
            #endregion

        }
        public void GroupNode(string groupID = null, string entranceID = null, string exitID = null)
        {
            if (Network.SelectedNodes.Items.Count() == 0) { return; }
            var groupBinding = (GroupIOBinding)grouper.MergeIntoGroup(Network,Network.SelectedNodes.Items);
            if (groupBinding == null)
            {
                return;
            }
            ((GroupNode)groupBinding.GroupNode).IOBinding = groupBinding;
            ((GroupSubnetIONodeViewModel)groupBinding.EntranceNode).IOBinding = groupBinding;
            ((GroupSubnetIONodeViewModel)groupBinding.ExitNode).IOBinding = groupBinding;
            if(groupID != null)
            {
                ((GroupNode)groupBinding.GroupNode).ID = groupID;
                ((GroupSubnetIONodeViewModel)groupBinding.EntranceNode).ID = entranceID;
                ((GroupSubnetIONodeViewModel)groupBinding.ExitNode).ID = exitID;
            }
        }
        public void UnGroupNode()
        {
            if (Network.SelectedNodes.Items.Count() == 0) { return; }
            var selectedGroupNode = Network.SelectedNodes.Items.First() as GroupNode;
            if (selectedGroupNode != null)
            {
                grouper.Ungroup(selectedGroupNode.IOBinding);
            }
            
        }
        public void LoadHMIData(JArray HMIData)
        {
            
            
        }
        public void LoadGroupData(JArray groupData, List<string> listGroupNewID = null, List<string> listGroupOldID = null)
        {
            foreach (var subNodeData in groupData)
            {
                #region GroupNode
                /*
                 * Group New Node into GroupNode
                 */
                Network.ClearSelection();
                string entranceNodeID = null, exitNodeID = null;
                foreach (var nodeData in subNodeData["nodeDatas"]) //group node
                {
                    string ID = nodeData["ID"].ToString();
                    string type = nodeData["Type"].ToString();
                    if (type == "BaseNode")
                    {
                        Network.Nodes.Items.Where(node => (node as BaseNode).ID == ID).First().IsSelected = true;
                    }
                    else
                    {
                        if (nodeData["isEntranceNode"].ToString() == "true")
                        {
                            entranceNodeID = nodeData["ID"].ToString();
                        }
                        else
                        {
                            exitNodeID = nodeData["ID"].ToString();
                        }
                    }
                }
                GroupNode(subNodeData["GroupID"].ToString(), entranceNodeID, exitNodeID);
                #endregion

                #region Position for Subnet Node
                var selectedGroupNode = (GroupNode)Network.Nodes.Items
                    .Where(group => (group as BaseNode).ID == subNodeData["GroupID"].ToString()).First();
                foreach (var nodeData in subNodeData["nodeDatas"])
                {
                    string ID = nodeData["ID"].ToString();
                    double x = Double.Parse(nodeData["x"].ToString());
                    double y = Double.Parse(nodeData["y"].ToString());
                    string type = nodeData["Type"].ToString();
                    if (type == "BaseNode")
                    {
                        selectedGroupNode.IOBinding.SubNetwork.Nodes.Items
                            .Where(node => (node as BaseNode).ID == ID)
                            .First().Position = new System.Windows.Point(x, y);
                    }
                    else
                    {
                        selectedGroupNode.IOBinding.SubNetwork.Nodes.Items
                            .Where(node => (node as BaseNode).ID == ID)
                            .First().Position = new System.Windows.Point(x, y);
                    }

                }
                #endregion

                #region IOGroupData
                /*
                 * Create/Copy IO of GroupNode and Create connection of GroupNode with other Node
                 */
                selectedGroupNode.Inputs.Clear();
                selectedGroupNode.Outputs.Clear();
                List<int> entranceNodeIndex = new List<int>();
                List<int> exitNodeIndex = new List<int>();
                foreach (var IOPortData in subNodeData["IOGroupData"])
                {
                    string inputID = IOPortData["inputID"].ToString();
                    string outputID = IOPortData["outputID"].ToString();
                    string outputPortType = IOPortData["outputPortType"].ToString();
                    string inputPortType = IOPortData["inputPortType"].ToString();
                    string type = IOPortData["Type"].ToString();
                    int index = int.Parse(IOPortData["Index"].ToString());
                    if (type == "EntranceNode")
                    {
                        if(listGroupNewID != null)
                        {
                            int _index = listGroupOldID.IndexOf(outputID);
                            outputID = _index != -1? listGroupNewID.ElementAt(listGroupOldID.IndexOf(outputID)) : outputID;
                        }
                        var parentOutputPort = Network.Nodes.Items.Where(node => (node as BaseNode).ID == outputID)
                            .Select(node => node.Outputs.Items.Where(port => port.PortType == outputPortType).FirstOrDefault()).FirstOrDefault();
                        if(parentOutputPort == null) { continue; }
                        if(entranceNodeIndex.IndexOf(index) == -1)
                        {
                            selectedGroupNode.AddEndpointDropPanelVM.NodeGroupIOBinding.AddNewGroupNodeInput(parentOutputPort);
                            entranceNodeIndex.Add(index);
                        }
                        Network.Connections.Add(Network.ConnectionFactory(selectedGroupNode.Inputs.Items
                            .Where(port => port.Name == inputPortType).ElementAt(index), parentOutputPort));
                    }
                    else
                    {
                        if (listGroupNewID != null)
                        {
                            int _index = listGroupOldID.IndexOf(outputID);
                            inputID = _index != -1 ? listGroupNewID.ElementAt(listGroupOldID.IndexOf(inputID)) : inputID;
                        }
                        var childInputPort = Network.Nodes.Items.Where(node => (node as BaseNode).ID == inputID)
                            .Select(node => node.Inputs.Items.Where(port => port.Name == inputPortType).FirstOrDefault()).FirstOrDefault();
                        if(childInputPort == null) { continue; }
                        if(exitNodeIndex.IndexOf(index) == -1)
                        {
                            selectedGroupNode.AddEndpointDropPanelVM.NodeGroupIOBinding.AddNewGroupNodeOutput(childInputPort);
                            exitNodeIndex.Add(index);
                        }
                        if (childInputPort.Connections.Count > 0) { continue; }
                        Network.Connections.Add(Network.ConnectionFactory(childInputPort, selectedGroupNode.Outputs.Items
                            .Where(port => port.PortType == outputPortType).ElementAt(index)));
                    }
                }
                #endregion

                #region subnetConnection
                /*
                 * Create/Copy IO of GroupSubnetIONode (Input / Output Node inside GroupNode) and Create connection of Node inside GroupNode
                 */
                foreach (var connection in subNodeData["subnetConnection"])
                {
                    string inputID = connection["inputID"].ToString();
                    string outputID = connection["outputID"].ToString();
                    string outputPortType = connection["outputPortType"].ToString();
                    string inputPortType = connection["inputPortType"].ToString();
                    int index = int.Parse(connection["Index"].ToString());
                    string type = connection["Type"].ToString();
                    BaseNode input_node = selectedGroupNode.IOBinding.SubNetwork.Nodes.Items.Where(node => (node as BaseNode).ID == inputID).First() as BaseNode;
                    BaseNode output_node = selectedGroupNode.IOBinding.SubNetwork.Nodes.Items.Where(node => (node as BaseNode).ID == outputID).First() as BaseNode;
                    //var inputPort = input_node.Inputs.Items.Where(port => port.Name == inputPortType).First();
                    //if (inputPort.Connections.Count > 0) { continue; }
                    if (type == "EntranceNode")
                    {
                        var inputPort = input_node.Inputs.Items.Where(port => port.PortType == inputPortType).First();
                        if (output_node.Outputs.Items.Count() <= index || ((output_node.Outputs.Items.Count() == 0)&&(index ==0)))
                        {                           
                            selectedGroupNode.AddEndpointDropPanelVM.NodeGroupIOBinding.AddNewSubnetInlet(inputPort);
                        }
                        selectedGroupNode.Subnet.Connections.Add(new ConnectionViewModel(Network, 
                            input_node.Inputs.Items.Where(port => port.PortType == inputPortType).First(), 
                            output_node.Outputs.Items.Where(port => port.PortType == outputPortType).ElementAt(index)));

                    }
                    else
                    {
                        var outputPort = output_node.Outputs.Items.Where(port => port.PortType == outputPortType).First();
                        if (input_node.Inputs.Items.Count() <= index || ((input_node.Inputs.Items.Count() == 0) && (index == 0)))
                        {
                            selectedGroupNode.AddEndpointDropPanelVM.NodeGroupIOBinding.AddNewSubnetOutlet(outputPort);
                        }
                        selectedGroupNode.Subnet.Connections.Add(new ConnectionViewModel(Network, 
                            input_node.Inputs.Items.Where(port => port.PortType == inputPortType).ElementAt(index), 
                            output_node.Outputs.Items.Where(port => port.PortType == outputPortType).First()));
                    }
                }
                #endregion
            }
        }
        public JArray SaveGroupData()
        {
            List<GroupNode> listGroupNode = new List<GroupNode>();
            foreach (var node in Network.Nodes.Items)
            {
                if (node.GetType() == typeof(GroupNode))
                {
                    listGroupNode.Add(node as GroupNode);
                }
            }
            JArray groupData = new JArray();
            foreach (GroupNode group in listGroupNode)
            {
                JObject subNodeData = new JObject();
                #region nodeDatas for position
                /*
                 * Data for Create/Copy Node inside Group and set position
                 */
                JArray nodeDatas = new JArray();
                foreach (var node in group.IOBinding.SubNetwork.Nodes.Items) //position
                {
                    JObject nodeData = new JObject();
                    nodeData.Add("ID", (node as BaseNode).ID);
                    if (node.GetType() != typeof(GroupSubnetIONodeViewModel))
                    {
                        nodeData.Add("Type", "BaseNode");
                    }
                    else
                    {
                        nodeData.Add("Type", "GroupNode");
                        if ((node as GroupSubnetIONodeViewModel)._isEntranceNode)
                        {
                            nodeData.Add("isEntranceNode", "true");
                        }
                        else
                        {
                            nodeData.Add("isEntranceNode", "false");
                        }
                    }
                    nodeData.Add("x", node.Position.X.ToString());
                    nodeData.Add("y", node.Position.Y.ToString());
                    nodeDatas.Add(nodeData);
                }
                #endregion
                #region IOGroupData
                /*
                 * Data for Create/Copy IO of GroupNode and Create connection of GroupNode with other Node
                 */
                JArray IOGroupData = new JArray();
                foreach (var port in group.Inputs.Items)
                {
                    foreach (var c in port.Connections.Items)
                    {
                        JObject IOPortData = new JObject();
                        IOPortData.Add("inputID", (c.Input.Parent as BaseNode).ID);
                        IOPortData.Add("outputID", (c.Output.Parent as BaseNode).ID);
                        IOPortData.Add("inputPortType", c.Input.PortType);
                        IOPortData.Add("outputPortType", c.Output.PortType);
                        IOPortData.Add("Type", "EntranceNode");
                        IOPortData.Add("Index", group.Inputs.Items.IndexOf(port).ToString());
                        IOGroupData.Add(IOPortData);
                    }
                }
                foreach (var port in group.Outputs.Items)
                {
                    foreach (var c in port.Connections.Items)
                    {
                        JObject IOPortData = new JObject();
                        IOPortData.Add("inputID", (c.Input.Parent as BaseNode).ID);
                        IOPortData.Add("outputID", (c.Output.Parent as BaseNode).ID);
                        IOPortData.Add("inputPortType", c.Input.PortType);
                        IOPortData.Add("outputPortType", c.Output.PortType);
                        IOPortData.Add("Type", "ExitNode");
                        IOPortData.Add("Index", group.Outputs.Items.IndexOf(port).ToString());
                        IOGroupData.Add(IOPortData);
                    }
                }
                #endregion
                #region subnetConnection
                /*
                 * Data for Create/Copy IO of GroupSubnetIONode (Input / Output Node inside GroupNode) and Create connection of Node inside GroupNode
                 */
                JArray subnetConnection = new JArray();
                foreach (var output in group.IOBinding.EntranceNode.Outputs.Items)
                {
                    foreach (var c in output.Connections.Items)
                    {
                        JObject subnetData = new JObject();
                        subnetData.Add("inputID", (c.Input.Parent as BaseNode).ID);
                        subnetData.Add("outputID", (c.Output.Parent as BaseNode).ID);
                        subnetData.Add("inputPortType", c.Input.PortType);
                        subnetData.Add("outputPortType", c.Output.PortType);
                        subnetData.Add("Type", "EntranceNode");
                        subnetData.Add("Index", group.IOBinding.EntranceNode.Outputs.Items.IndexOf(output).ToString());
                        subnetConnection.Add(subnetData);
                    }
                }
                foreach (var input in group.IOBinding.ExitNode.Inputs.Items)
                {
                    foreach (var c in input.Connections.Items)
                    {
                        JObject subnetData = new JObject();
                        subnetData.Add("inputID", (c.Input.Parent as BaseNode).ID);
                        subnetData.Add("outputID", (c.Output.Parent as BaseNode).ID);
                        subnetData.Add("inputPortType", c.Input.PortType);
                        subnetData.Add("outputPortType", c.Output.PortType);
                        subnetData.Add("Type", "ExitNode");
                        subnetData.Add("Index", group.IOBinding.ExitNode.Inputs.Items.IndexOf(input).ToString());
                        subnetConnection.Add(subnetData);
                    }
                }
                #endregion
                subNodeData.Add("subnetConnection", subnetConnection);
                subNodeData.Add("IOGroupData", IOGroupData);
                subNodeData.Add("nodeDatas", nodeDatas);
                subNodeData.Add("GroupID", group.ID);
                groupData.Add(subNodeData);
                grouper.Ungroup(group.IOBinding);

            }
            return groupData;
        }

        public void ChangeNodeName() //Test
        {
            var selectedNode = Network.SelectedNodes?.Items?.ToList();
            if(selectedNode.Count() == 1)
            {
                selectedNode.ElementAt(0).Name = "Changed";
            }
        }

        public void CopyNodeFolder(NodeViewModel node)
        {
            MainViewModel mvm = (Application.Current.MainWindow.DataContext as MainViewModel);
            string fileName = "test.txt";
            string sourcePath = System.IO.Path.Combine(mvm.CopyDir, (node as BaseNode).ID);
            string targetPath = System.IO.Path.Combine(BaseDir, (node as BaseNode).ID);
            System.IO.Directory.CreateDirectory(targetPath);
            // Use Path class to manipulate file and directory paths.
            //string sourceFile = System.IO.Path.Combine(sourcePath, fileName);
            string destFile;
            if (System.IO.Directory.Exists(sourcePath))
            {
                string[] files = System.IO.Directory.GetFiles(sourcePath);

                // Copy the files and overwrite destination files if they already exist.
                foreach (string s in files)
                {
                    // Use static Path methods to extract only the file name from the path.
                    fileName = System.IO.Path.GetFileName(s);
                    destFile = System.IO.Path.Combine(targetPath, fileName);
                    System.IO.File.Copy(s, destFile, true);
                }
            }
        }

        public void CopySegmentFolder(NodeViewModel node,NodeViewModel oldnode=null)
        {
            MainViewModel mvm = (Application.Current.MainWindow.DataContext as MainViewModel);
            string fileName = "test.txt";
            if (oldnode == null)
            {
                oldnode = node;
            }
            string sourcePath = System.IO.Path.Combine(mvm.CopyDir, (oldnode as BaseNode).ID);
            string targetPath = System.IO.Path.Combine(BaseDir, (node as BaseNode).ID);

            DirectoryCopy(sourcePath, targetPath, true);
        }

        private static void DirectoryCopy(string sourceDirName, string destDirName, bool copySubDirs)
        {
            // Get the subdirectories for the specified directory.
            DirectoryInfo dir = new DirectoryInfo(sourceDirName);

            if (!dir.Exists)
            {
                throw new DirectoryNotFoundException(
                    "Source directory does not exist or could not be found: "
                    + sourceDirName);
            }

            DirectoryInfo[] dirs = dir.GetDirectories();

            // If the destination directory doesn't exist, create it.       
            Directory.CreateDirectory(destDirName);

            // Get the files in the directory and copy them to the new location.
            FileInfo[] files = dir.GetFiles();
            foreach (FileInfo file in files)
            {
                string tempPath = Path.Combine(destDirName, file.Name);
                file.CopyTo(tempPath, true);
            }

            // If copying subdirectories, copy them and their contents to new location.
            if (copySubDirs)
            {
                foreach (DirectoryInfo subdir in dirs)
                {
                    string tempPath = Path.Combine(destDirName, subdir.Name);
                    DirectoryCopy(subdir.FullName, tempPath, copySubDirs);
                }
            }
        }
        public string ConfigDir { get; set; }
        public string HMISettingDir { get; set; }
        public DesignerHost(string dir)
        {
            NetworkBreadcrumbBar.ActivePath.Add(new NetworkBreadcrumb
            {
                Name = "Main",
                Network = Network
            });
            NetworkBreadcrumbBar.PropertyChanged += NetworkBreadcrumbBar_PropertyChanged;

            BaseDir = System.IO.Path.Combine(dir, "designer");
            if (!System.IO.Directory.Exists(BaseDir))
            {
                System.IO.Directory.CreateDirectory(BaseDir);
            }
            ConfigDir = System.IO.Path.Combine(BaseDir, "config.json");
            HMISettingDir = System.IO.Path.Combine(BaseDir, "hmi.json");
            Initialize();

            grouper = new NodeGrouper
            {
                GroupNodeFactory = (subnet) => new GroupNode(subnet),
                EntranceNodeFactory = () => new GroupSubnetIONodeViewModel(Network, true, false) { Name = "Group Input" },
                ExitNodeFactory = () => new GroupSubnetIONodeViewModel(Network, false, true) { Name = "Group Output" },
                SubNetworkFactory = () => new NetworkViewModel(),
                IOBindingFactory = (groupNode, entranceNode, exitNode) =>
                    new GroupIOBinding(groupNode, entranceNode, exitNode)
            };
            var isGroupNodeSelected = this.WhenAnyValue(vm => vm.Network)
                .Select(net => net.SelectedNodes.Connect())
                .Switch()
                .Select(_ => Network.SelectedNodes.Count == 1 && Network.SelectedNodes.Items.First() is GroupNode);
            
        }

        private void NetworkBreadcrumbBar_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            //Network = (NetworkBreadcrumbBar.ActiveItem as NetworkBreadcrumb).Network;
        }

        public DesignerHost()
        {
            this.HMI.host = this;
            var dir = Workspace.WorkspaceManager.Instance.jobDataPath;
            Network = new NetworkViewModel();
            BaseDir = System.IO.Path.Combine(dir, "designer");
            if (!System.IO.Directory.Exists(BaseDir))
            {
                System.IO.Directory.CreateDirectory(BaseDir);
            }
            Initialize();
            
        }

        public static string GenID()
        {
            return Guid.NewGuid().ToString();
        }
        public BaseNode AddNode(string typename, string id = "")
        {
            if (id == "") { id = GenID(); }
            foreach(var item in nodeTypes)
            {
                if(item == typename)
                {
                    Type t = Type.GetType("NOVisionDesigner.Designer.Nodes."+item);
                    var created= Activator.CreateInstance(t,new object[] {this, BaseDir,id});
                    if(created is BaseNode)
                    {
                        Console.WriteLine("Created tool " + typename + " with ID: " + (created as BaseNode).ID);
                        Network.Nodes.Add(created as BaseNode);
                       
                        return (created as BaseNode);
                    }
                }
            }
            return null;
        }
        public BaseNode AddNodeDynamicInput(string typename, JArray connections,string id = "")
        {
            if (id == "") { id = GenID(); }
            foreach (var item in nodeTypes)
            {
                if (item == typename)
                {
                    Type t = Type.GetType("NOVisionDesigner.Designer.Nodes." + item);
                    var created = Activator.CreateInstance(t, new object[] { this, BaseDir, id });
                    if (created is BaseNode)
                    {
                        Console.WriteLine("Created tool " + typename + " with ID: " + (created as BaseNode).ID);
                        foreach (var connection in connections)
                        {
                            string inputID = connection["inputID"].ToString();
                            string outputID = connection["outputID"].ToString();
                            string outputPortType = connection["outputPortType"].ToString();
                            string inputPortType = connection["inputPortType"].ToString();
                            if(inputID == id)
                            {
                                switch (outputPortType)
                                {

                                    case "Image":
                                        {
                                            ValueNodeInputViewModel<HImage> input = new ValueNodeInputViewModel<HImage>()
                                            {
                                                Name = inputPortType,
                                            };

                                            (created as BaseNode).Inputs.Add(input);

                                            break;
                                        }
                                    case "Region":
                                        {


                                            break;
                                        }
                                    case "String":
                                        {


                                            break;
                                        }
                                    case "Interger":
                                        {


                                            break;
                                        }
                                    default:
                                        break;
                                }
                               
                            }
                          
                        }
                        Network.Nodes.Add(created as BaseNode);

                        return (created as BaseNode);
                    }
                }
            }
            return null;
        }
        public void OnRunComplete(HImage image)
        {

        }
        public NodeViewModel FindNodeByID(string ID)
        {
           
            foreach (var item in Network.Nodes.Items)
            {
                if (item is BaseNode)
                {
                    if ((item as BaseNode).ID == ID)
                    {
                        return item;
                    }
                }
                if(item is GroupNode)
                {
                    foreach(var subNode in (item as GroupNode).IOBinding.SubNetwork.Nodes.Items)
                    {
                        if ((subNode as BaseNode).ID == ID)
                        {
                            return subNode;
                        }
                    }
                }
            }
            return null;
        }
        public void RemoveNode(string id)
        {
            var node = FindNodeByID(id);
            if (node != null)
            {
                Network.Nodes.Remove(node);
            }
            
        }
        public void RemoveNode(NodeViewModel node)
        {
            
            if (node != null)
            {
                Network.Nodes.Remove(node);
                node.OnRemove();
            }

        }
        public void RegisterDisplay(IEnumerable<HWindow> displays)
        {
            ListDisplay.AddRange(displays);
        }
        public void RegisterDisplay(HWindow display)
        {
            ListDisplay.Add(display);
        }
        public ObservableCollection<HWindow> ListDisplay { get; set; } = new ObservableCollection<HWindow>();

    }
    [JsonObject]
    public class DisplaySetting:ReactiveObject
    {
        private Rect _image_part;
        
        public Rect ImagePart
        {
            get
            {
                return _image_part;
            }
            set
            {
                this.RaiseAndSetIfChanged(ref _image_part, value);
            }
        }
        private bool _show_live_image;
        public bool ShowLiveImage
        {
            get
            {
                return _show_live_image;
            }
            set
            {
                this.RaiseAndSetIfChanged(ref _show_live_image, value);
            }
        }
        private bool _show_last_fail=true ;
        public bool ShowLastFail
        {
            get
            {
                return _show_last_fail;
            }
            set
            {
                this.RaiseAndSetIfChanged(ref _show_last_fail, value);
            }
        }
        private DisplayLayout _layout = DisplayLayout.Horizontal;
        public DisplayLayout Layout
        {
            get
            {
                return _layout;
            }
            set
            {
                this.RaiseAndSetIfChanged(ref _layout, value);
            }
        }
        public double Width { get; set; } = 100;
        public double Height { get; set; } = 100;
        public double PosX { get; set; } = 0;
        public double PosY { get; set; } = 0;
    }
    public enum DisplayLayout
    {
        Vertical,Horizontal
    }
    public class AddNodeFactory
    {
        public override string ToString()
        {
            return Type;
        }
        public Func<NodeViewModel> AddFunction { get; set; }
        public string Type { get; set; }
        public string TypeNode { get; set; }
        public string DisplayName { get; set; }
        public string Description { get; set; }
        public int Sortindex { get; set; }
        public BitmapImage Icon { get; set; }
        public AddNodeFactory(string Type, string TypeNode, string Icon,int SortIndex ,Func<NodeViewModel> AddFunction)
        {
            if (Icon != null&Icon!="")
            {
                try
                {
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        this.Icon = new BitmapImage(ResourceAccessor.Get(Icon));
                    });
                    
                }catch(Exception ex)
                {

                }
                
            }
            this.Sortindex = Sortindex;
            this.Type = Type;
            this.TypeNode = TypeNode;
            this.AddFunction = AddFunction;
        }
        internal static class ResourceAccessor
        {
            public static Uri Get(string resourcePath)
            {
                var uri = string.Format(
                    "pack://application:,,,/{0};component/{1}"
                    , Assembly.GetExecutingAssembly().GetName().Name
                    , resourcePath
                );

                return new Uri(uri);
            }
        }
    }
    /// <summary>
    /// interface to update display everytime complete running node graph
    /// </summary>


}
