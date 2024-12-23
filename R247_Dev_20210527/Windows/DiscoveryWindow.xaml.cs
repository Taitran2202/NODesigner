using NetMQ;
using NOVisionDesigner.Designer.Communication;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using static NOVisionDesigner.Designer.Communication.Bus;

namespace NOVisionDesigner.Windows
{
    /// <summary>
    /// Interaction logic for DiscoveryWindow.xaml
    /// </summary>
    public partial class DiscoveryWindow : Window
    {
        private object _syncLock = new Object();
        public DiscoveryWindow()
        {
            InitializeComponent();
            Discover();
            //Discover();
        }
        bool is_run = true;
        NetMQPoller poller;
        public void Discover()
        {
            // Create a bus using broadcast port 9999
            // All communication with the bus is through the returned actor
            var bus = Bus.Create(9999);
            var Collection = CollectionViewSource.GetDefaultView(bus.m_nodes);
            BindingOperations.EnableCollectionSynchronization(bus.m_nodes, _syncLock);
            lst_nodes.ItemsSource = Collection;
            Task.Run(() =>
            {
                var actor = bus.m_actor;
                poller = bus.m_poller;
                actor.SendFrame(Bus.GetHostAddressCommand);
                var hostAddress = actor.ReceiveFrameString();

                //Console.Title = $"NetMQ Beacon Demo at {hostAddress}";

                // beacons publish every second, so wait a little longer than that to
                // let all the other nodes connect to our new node
                Thread.Sleep(1100);

                // publish a hello message
                // note we can use NetMQSocket send and receive extension methods
                actor.SendMoreFrame(Bus.PublishCommand).SendMoreFrame("Hello?").SendFrame(hostAddress);

                // receive messages from other nodes on the bus
                while (is_run)
                {
                    // actor is receiving messages forwarded by the Bus subscriber
                    string message = actor.ReceiveFrameString();
                    switch (message)
                    {
                        case "Hello?":
                            // another node is saying hello
                            var fromHostAddress = actor.ReceiveFrameString();
                            var msg = fromHostAddress + " says Hello?";
                            Console.WriteLine(msg);

                            // send back a welcome message via the Bus publisher
                            msg = hostAddress + " says Welcome!";
                            actor.SendMoreFrame(Bus.PublishCommand).SendFrame(msg);
                            break;
                        case Bus.AddedNodeCommand:
                            var addedAddress = actor.ReceiveFrameString();
                            Console.WriteLine("Added node {0} to the Bus", addedAddress);
                            break;
                        case Bus.RemovedNodeCommand:
                            var removedAddress = actor.ReceiveFrameString();
                            Console.WriteLine("Removed node {0} from the Bus", removedAddress);
                            break;
                        default:
                            // it's probably a welcome message
                            Console.WriteLine(message);
                            break;
                    }
                }
            });

        }
        protected override void OnClosed(EventArgs e)
        {
            is_run = false;
            poller.Stop();
            base.OnClosed(e);
        }
    }
    public class ViewModel
    {
        public ObservableCollection<NodeKey> DataItems { get; set; }

        public ViewModel()
        {
            DataItems = InitData();
        }
        public ObservableCollection<NodeKey> InitData()
        {
            ObservableCollection<NodeKey> DataItems = new ObservableCollection<NodeKey>();
            DataItems.Add(new NodeKey() { LastActiveTime = DateTime.Now, Data = new NodeData() { Jobs = new ObservableCollection<JobDetail>() { new JobDetail() } } });
            return DataItems;
        }

    }
}
