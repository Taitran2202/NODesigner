using DynamicData;
using HalconDotNet;
using MySql.Data.MySqlClient;
using NodeNetwork.Toolkit.ValueNode;
using NOVisionDesigner.Designer.Editors;
using NOVisionDesigner.Designer.Misc;
using NOVisionDesigner.Designer.NodeViews;
using NOVisionDesigner.Designer.Windows;
using ReactiveUI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;

namespace NOVisionDesigner.Designer.Nodes
{
    [NodeInfo("DatabaseNode", "DatabaseNode")]
    public class DatabaseNode : BaseNode
    {
        public override void Save(HFile file)
        {
            HelperMethods.SaveParam(file, this);
        }

        public override void Load(DeserializeFactory item)
        {
            HelperMethods.LoadParam(item, this);
        }
        static DatabaseNode()
        {
            Splat.Locator.CurrentMutable.Register(() => new NodeViewWithEditor(), typeof(IViewFor<DatabaseNode>));
        }

        public ValueNodeInputViewModel<bool> TriggerInput { get; }
        public ValueNodeOutputViewModel<MySqlConnection> Connection { get; }

        #region Properties
        [HMIProperty("Open database setting")]
        public IReactiveCommand OpenEditor
        {
          get { return ReactiveCommand.Create((Control sender) => OnCommand("editor",sender)); }
        }        

        
        #endregion

        public override void OnCommand(string type,Control sender)
        {
            switch (type)
            {
                case "editor":
                    
                    break;
            }
        }
        [HMIProperty("Connection String")]
        public string ConnectionString { get; set; }
        public bool Connected { get; set; }
        MySqlConnection conn;
        public override void OnLoadComplete()
        {
            try
            {
                conn = new MySqlConnection(ConnectionString);
                conn.Open();
                Connected = true;
            }
            catch (Exception ex)
            {
                Connected = false;
            }
            
        }
        public override void OnInitialize()
        {
        }
        public override void Run(object context)
        {
            if (Connected)
            {
                Connection.OnNext(conn);
            }
        }
        public DatabaseNode(DesignerHost designer, string dir, string id) : base(designer, dir, id)
        {
            Name = "Database Node";
            TriggerInput = new ValueNodeInputViewModel<bool>()
            {
                Name = "TriggerInput",
                PortType = "Boolean"
            };
            this.Inputs.Add(TriggerInput);

            Connection = new ValueNodeOutputViewModel<MySqlConnection>()
            {
                Name = "Connection",
                PortType = "MySqlConnection"
            };
            this.Outputs.Add(Connection);


        }

       
    }
    
}
