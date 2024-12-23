using NetMQ;
using NetMQ.Sockets;
using Newtonsoft.Json;
using NOVisionDesigner.ViewModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NOVisionDesigner.Designer.Communication
{
    public class HMIService:IDisposable
    {
        public void Dispose()
        {
            poller.Stop();
            responseSocket.Close();
        }
        
        public const string GetTagListCommand = "GetTagList";
        public const string GetTagAttributeCommand = "GetTagAttribute";

        string HostAddressPublish = "tcp://*:3000";
        string HostAddressResponse = "tcp://*:3001";
        PublisherSocket pubSocket;
        ResponseSocket responseSocket;
        MainViewModel model;
        NetMQPoller poller;
        public HMIService(MainViewModel model)
        {
            this.model = model;
            pubSocket = new PublisherSocket();
            pubSocket.Bind(HostAddressPublish);

            responseSocket = new ResponseSocket();
            responseSocket.Bind(HostAddressResponse);
            //responseSocket.ReceiveReady += ResponseSocket_ReceiveReady;
            poller=new NetMQPoller { responseSocket };
            responseSocket.ReceiveReady += (o, e) =>
            {
                var message = responseSocket.ReceiveFrameString();
                try
                {
                    var request = JsonConvert.DeserializeObject<RequestMessage>(message);
                    if (request != null)
                    {
                        switch (request.Command)
                        {
                            case GetTagListCommand:
                                var testlist = new List<HMITag>();
                                foreach (var item in model.VisionManager.VisionModels)
                                {
                                    var list = item.Designer.GetTagList();
                                    testlist.AddRange(list);
                                }
                                responseSocket.SendFrame(JsonConvert.SerializeObject(testlist));
                                break;
                        }
                    }
                }
                catch (Exception ex)
                {

                }
            };
            poller.RunAsync();
            
        }
        public void Publish(string topic,string message)
        {
            pubSocket.SendMoreFrame(topic).SendFrame(message);
        }
        public void Publish(string topic, byte[] message)
        {
            pubSocket.SendMoreFrame(topic).SendFrame(message);
        }
        
    }
    public class HMITag
    {
        
        public string address { get; set; } = "345";
        public string name { get; set; } = "789";
        public int type { get; set; } = 2;
    }
    public class RequestMessage
    {
        public string Command { get; set; }
        public string Payload { get; set; }

    }
}
