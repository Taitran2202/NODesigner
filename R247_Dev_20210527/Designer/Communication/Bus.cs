using NetMQ;
using NetMQ.Sockets;
using NOVisionDesigner.Windows;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;

namespace NOVisionDesigner.Designer.Communication
{
    internal class Bus
    {
        // Actor Protocol
        public const string PublishCommand = "P";
        public const string GetHostAddressCommand = "GetHostAddress";
        public const string AddedNodeCommand = "AddedNode";
        public const string RemovedNodeCommand = "RemovedNode";

        // Dead nodes timeout
        private readonly TimeSpan m_deadNodeTimeout = TimeSpan.FromSeconds(10);

        // we will use this to check if we already know about the node
       

        private readonly int m_broadcastPort;

        public readonly NetMQActor m_actor;

        private PublisherSocket m_publisher;
        private SubscriberSocket m_subscriber;
        private NetMQBeacon m_beacon;
        public NetMQPoller m_poller;
        private PairSocket m_shim;
        public  readonly ObservableCollection<NodeKey> m_nodes; // value is the last time we "saw" this node
        private int m_randomPort;
        //private object lockobject = new object();
        private Bus(int broadcastPort)
        {
            m_nodes = new ObservableCollection<NodeKey>();
            //BindingOperations.EnableCollectionSynchronization(m_nodes, lockobject);
            m_broadcastPort = broadcastPort;
            m_actor = NetMQActor.Create(RunActor);
            
        }

        /// <summary>
        /// Creates a new message bus actor. All communication with the bus is
        /// through the returned <see cref="NetMQActor"/>.
        /// </summary>
        public static Bus Create(int broadcastPort)
        {
            Bus node = new Bus(broadcastPort);
            return node;
        }

        private void RunActor(PairSocket shim)
        {
            // save the shim to the class to use later
            m_shim = shim;

            // create all subscriber, publisher and beacon
            using (m_subscriber = new SubscriberSocket())
            using (m_publisher = new PublisherSocket())
            using (m_beacon = new NetMQBeacon())
            {
                // listen to actor commands
                m_shim.ReceiveReady += OnShimReady;

                // subscribe to all messages
                m_subscriber.Subscribe("");

                // we bind to a random port, we will later publish this port
                // using the beacon
                m_randomPort = m_subscriber.BindRandomPort("tcp://*");
                Console.WriteLine("Bus subscriber is bound to {0}", m_subscriber.Options.LastEndpoint);

                // listen to incoming messages from other publishers, forward them to the shim
                m_subscriber.ReceiveReady += OnSubscriberReady;

                // configure the beacon to listen on the broadcast port
                Console.WriteLine("Beacon is being configured to UDP port {0}", m_broadcastPort);
                m_beacon.Configure(m_broadcastPort);

                // publishing the random port to all other nodes
                Console.WriteLine("Beacon is publishing the Bus subscriber port {0}", m_randomPort);
                m_beacon.Publish(m_randomPort.ToString(), TimeSpan.FromSeconds(1));

                // Subscribe to all beacon on the port
                Console.WriteLine("Beacon is subscribing to all beacons on UDP port {0}", m_broadcastPort);
                m_beacon.Subscribe("");

                // listen to incoming beacons
                m_beacon.ReceiveReady += OnBeaconReady;

                // Create a timer to clear dead nodes
                NetMQTimer timer = new NetMQTimer(TimeSpan.FromSeconds(1));
                timer.Elapsed += ClearDeadNodes;

                // Create and configure the poller with all sockets and the timer
                m_poller = new NetMQPoller { m_shim, m_subscriber, m_beacon, timer }; 

                // signal the actor that we finished with configuration and
                // ready to work
                m_shim.SignalOK();
                // polling until cancelled
                m_poller.Run();
            }
        }

        private void OnShimReady(object sender, NetMQSocketEventArgs e)
        {
            // new actor command 
            string command = m_shim.ReceiveFrameString();

            // check if we received end shim command
            if (command == NetMQActor.EndShimMessage)
            {
                // we cancel the socket which dispose and exist the shim
                m_poller.Stop();
            }
            else if (command == PublishCommand)
            {
                // it is a publish command
                // we just forward everything to the publisher until end of message
                NetMQMessage message = m_shim.ReceiveMultipartMessage();
                m_publisher.SendMultipartMessage(message);
            }
            else if (command == GetHostAddressCommand)
            {
                var address = m_beacon.BoundTo + ":" + m_randomPort;
                m_shim.SendFrame(address);
            }
        }

        private void OnSubscriberReady(object sender, NetMQSocketEventArgs e)
        {
            // we got a new message from the bus
            // let's forward everything to the shim
            NetMQMessage message = m_subscriber.ReceiveMultipartMessage();
            m_shim.SendMultipartMessage(message);
        }

        private void OnBeaconReady(object sender, NetMQBeaconEventArgs e)
        {
            // we got another beacon
            // let's check if we already know about the beacon
            var message = m_beacon.Receive();
            int port;
            int.TryParse(message.String, out port);

            NodeKey node = new NodeKey(message.PeerHost, port);

            // check if node already exist
            if (!m_nodes.Contains(node))
            {
                // we have a new node, let's add it and connect to subscriber
                node.LastActiveTime = DateTime.Now;
                m_nodes.Add(node);
                m_publisher.Connect(node.Address);
                m_shim.SendMoreFrame(AddedNodeCommand).SendFrame(node.Address);
            }
            else
            {
                //Console.WriteLine("Node {0} is not a new beacon.", node);
                node.LastActiveTime = DateTime.Now;
                node.Status = NodeStatus.Active;
            }
        }

        private void ClearDeadNodes(object sender, NetMQTimerEventArgs e)
        {
            //create an array with the dead nodes
            var deadNodes = new List<NodeKey>();
            foreach(var n in m_nodes)
            {
                if( DateTime.Now > n.LastActiveTime + m_deadNodeTimeout)
                {
                    deadNodes.Add(n);
                }
            }
           //var deadNodes = m_nodes.GetEnumerator().
           //    Where(n => DateTime.Now > n.Value + m_deadNodeTimeout)
           //    .Select(n => n.Key).ToArray();

            // remove all the dead nodes from the nodes list and disconnect from the publisher
            foreach (var node in deadNodes)
            {
                //m_nodes.Remove(node);
                if( node.Status == NodeStatus.Active)
                {
                    node.Status = NodeStatus.Unactive;
                    m_publisher.Disconnect(node.Address);
                    m_shim.SendMoreFrame(RemovedNodeCommand).SendFrame(node.Address);
                }

            }
        }
    }
    public class NodeKey : INotifyPropertyChanged
    {

        void RaisePropertyChanged(string prop)
        {
            if (PropertyChanged != null) { PropertyChanged(this, new PropertyChangedEventArgs(prop)); }
        }
        public event PropertyChangedEventHandler PropertyChanged;

        public NodeKey(string name, int port)
        {
            Name = name;
            Port = port;
            Address = $"tcp://{name}:{port}";
            HostName = Dns.GetHostEntry(name).HostName;
            Status = NodeStatus.Active;
        }
        public NodeKey()
        {
            Name = "test";
            Port = 1234;
            Address = $"tcp://{Name}:{Port}";
            //HostName = Dns.GetHostEntry(name).HostName;
            Status = NodeStatus.Active;
        }
        DateTime _last_active_time;
        public DateTime LastActiveTime
        {
            get
            {
                return _last_active_time;
            }
            set
            {
                if (_last_active_time != value)
                {
                    _last_active_time = value;
                    RaisePropertyChanged("LastActiveTime");
                }
            }
        }

        public string Name { get; }
        public int Port { get; }
        public NodeData Data { get; set; } = new NodeData();
        public string Address { get; }

        public string HostName { get; private set; }
        NodeStatus _status;
        public NodeStatus Status
        {
            get
            {
                return _status;
            }
            set
            {
                if (_status != value)
                {
                    _status = value;
                    RaisePropertyChanged("Status");
                }
            }
        }

        protected bool Equals(NodeKey other)
        {
            return string.Equals(Name, other.Name) && Port == other.Port;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((NodeKey)obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return ((Name?.GetHashCode() ?? 0) * 397) ^ Port;
            }
        }

        public override string ToString()
        {
            return Address;
        }
    }
    public enum NodeStatus
    {
        Active,Unactive
    }
    public enum RunningStatus
    {
        Online, Offline
    }
    public class NodeData
    {
        public ObservableCollection<JobDetail> Jobs { get; set; } = new ObservableCollection<JobDetail>();
        public RunningStatus Status { get; set; }
    }
    public class ObservableKeyValuePair<TKey, TValue> : INotifyPropertyChanged
    {
        #region properties
        private TKey key;
        private TValue value;

        public TKey Key
        {
            get { return key; }
            set
            {
                key = value;
                OnPropertyChanged("Key");
            }
        }

        public TValue Value
        {
            get { return value; }
            set
            {
                this.value = value;
                OnPropertyChanged("Value");
            }
        }
        #endregion

        #region INotifyPropertyChanged Members

        [field: NonSerialized]
        public event PropertyChangedEventHandler PropertyChanged;

        public void OnPropertyChanged(string name)
        {
            PropertyChangedEventHandler handler = PropertyChanged;
            if (handler != null) handler(this, new PropertyChangedEventArgs(name));
        }

        #endregion
    }

    [Serializable]
    public class ObservableDictionary<TKey, TValue> : ObservableCollection<ObservableKeyValuePair<TKey, TValue>>, IDictionary<TKey, TValue>
    {

        #region IDictionary<TKey,TValue> Members

        public void Add(TKey key, TValue value)
        {
            if (ContainsKey(key))
            {
                throw new ArgumentException("The dictionary already contains the key");
            }
            base.Add(new ObservableKeyValuePair<TKey, TValue>() { Key = key, Value = value });
        }

        public bool ContainsKey(TKey key)
        {
            //var m=base.FirstOrDefault((i) => i.Key == key);
            var r = ThisAsCollection().FirstOrDefault((i) => Equals(key, i.Key));

            return !Equals(default(ObservableKeyValuePair<TKey, TValue>), r);
        }

        bool Equals<TKey>(TKey a, TKey b)
        {
            return EqualityComparer<TKey>.Default.Equals(a, b);
        }

        private ObservableCollection<ObservableKeyValuePair<TKey, TValue>> ThisAsCollection()
        {
            return this;
        }

        public ICollection<TKey> Keys
        {
            get { return (from i in ThisAsCollection() select i.Key).ToList(); }
        }

        public bool Remove(TKey key)
        {
            var remove = ThisAsCollection().Where(pair => Equals(key, pair.Key)).ToList();
            foreach (var pair in remove)
            {
                ThisAsCollection().Remove(pair);
            }
            return remove.Count > 0;
        }

        public bool TryGetValue(TKey key, out TValue value)
        {
            value = default(TValue);
            var r = GetKvpByTheKey(key);
            if (!Equals(r, default(ObservableKeyValuePair<TKey, TValue>)))
            {
                return false;
            }
            value = r.Value;
            return true;
        }

        private ObservableKeyValuePair<TKey, TValue> GetKvpByTheKey(TKey key)
        {
            return ThisAsCollection().FirstOrDefault((i) => i.Key.Equals(key));
        }

        public ICollection<TValue> Values
        {
            get { return (from i in ThisAsCollection() select i.Value).ToList(); }
        }

        public TValue this[TKey key]
        {
            get
            {
                TValue result;
                if (!TryGetValue(key, out result))
                {
                    throw new ArgumentException("Key not found");
                }
                return result;
            }
            set
            {
                if (ContainsKey(key))
                {
                    GetKvpByTheKey(key).Value = value;
                }
                else
                {
                    Add(key, value);
                }
            }
        }

        #endregion

        #region ICollection<KeyValuePair<TKey,TValue>> Members

        public void Add(KeyValuePair<TKey, TValue> item)
        {
            Add(item.Key, item.Value);
        }

        public bool Contains(KeyValuePair<TKey, TValue> item)
        {
            var r = GetKvpByTheKey(item.Key);
            if (Equals(r, default(ObservableKeyValuePair<TKey, TValue>)))
            {
                return false;
            }
            return Equals(r.Value, item.Value);
        }

        public void CopyTo(KeyValuePair<TKey, TValue>[] array, int arrayIndex)
        {
            throw new NotImplementedException();
        }

        public bool IsReadOnly
        {
            get { return false; }
        }

        public bool Remove(KeyValuePair<TKey, TValue> item)
        {
            var r = GetKvpByTheKey(item.Key);
            if (Equals(r, default(ObservableKeyValuePair<TKey, TValue>)))
            {
                return false;
            }
            if (!Equals(r.Value, item.Value))
            {
                return false;
            }
            return ThisAsCollection().Remove(r);
        }

        #endregion

        #region IEnumerable<KeyValuePair<TKey,TValue>> Members

        public new IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator()
        {
            return (from i in ThisAsCollection() select new KeyValuePair<TKey, TValue>(i.Key, i.Value)).ToList().GetEnumerator();
        }

        #endregion
    }
}
