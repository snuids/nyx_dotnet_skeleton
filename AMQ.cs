using System;
using System.Collections.Generic;
using System.Threading;
using Apache.NMS;
using Apache.NMS.ActiveMQ;
using Apache.NMS.ActiveMQ.Commands;
using Microsoft.Extensions.Configuration;  
using Microsoft.Extensions.DependencyInjection; 

namespace NYX_Skeleton
{
    public class AMQ
    {
        private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        IConnection _connection;
        ISession _session;
        Thread _main_thread;
        bool _mustrun=true;
        bool _connected=false;
        IMessageReceiver _receiver;
        IConfiguration _config;
        Dictionary<String,String> _amqconfig;
        Dictionary<string,IDestination> _destinations=new Dictionary<string,IDestination>();

        Object _lock=new Object();

        List<IMessageConsumer> _consumers=new List<IMessageConsumer>();

        public AMQ(IMessageReceiver receiver,IConfiguration config,Dictionary<String,String> amqconfig)
        {
            _receiver = receiver;
            _config = config;
            _amqconfig=amqconfig;

            
            _main_thread = new Thread(new ThreadStart(Running));
            _main_thread.Start();                                 
        }

        ~AMQ()
        {
            log.Info("In destructor to stop connection");
            Disconnect();
        }
    
        public void Disconnect()
        {
            lock(_lock)
            {
                try
                {
                    try{
                        foreach(IMessageConsumer cons in _consumers)
                        {
                            cons.Close();
                        }
                    }
                    catch(Exception e)
                    {
                        log.Error("Unable to stop consumer. Exc="+e.Message,e);
                    }
                    _session.Close();
                    _connection.Close();
                }
                catch(Exception e)
                {
                    log.Info("Unable to stop connection."+e.Message);
                }
                _connected=false;
            }
        }

        public void Running()
        {
            while(_mustrun)
            {
                if(!_connected)
                {
                    try
                    {
                        Connect();
                    }
                    catch(Exception e)
                    {
                        log.Info("Unable to connect. Exc="+e.Message,e);
                    }                    
                }
                if(_connected)
                {
                    foreach(IMessageConsumer cons in _consumers)
                    {
                        IMessage mes=cons.ReceiveNoWait();
                        while (mes!=null)
                        {
                            log.Info("======>");
                            String dest="/"+mes.NMSDestination.ToString().Replace(":/","");
                                                    
                            var headers=new Dictionary<string,Object>();
                            foreach(string prop in mes.Properties.Keys)
                            {
                                headers[prop]=mes.Properties[prop];
                            }
                            string textmes="NA";
                            if (mes is ActiveMQTextMessage)
                            {
                                textmes=((ActiveMQTextMessage)mes).Text;
                            }
                            else
                            {
                                ActiveMQMessage amsg=((ActiveMQTextMessage)mes);                                
                            }
                            try
                            {
                                _receiver.MessageReceived(dest,textmes,headers);
                            }
                            catch(Exception e)
                            {
                                log.Error("Crash during message handling");                                
                            }
                            mes=cons.ReceiveNoWait();
                        }
                    }
                }
                System.Threading.Thread.Sleep(1000);

                log.Info("In AMQC main thread");
                if(_connected)
                   SendMessage("/queue/TESTQ11","TOTO"); 
            }
            
        }
        public void Connect()
        {
            lock(_lock)
            {
                var url="tcp://"+_amqconfig["AMQC_URL"]+":"+_amqconfig["AMQC_PORT"];
                log.Info(">>>>>> Connection to:"+url+" with user:"+_amqconfig["AMQC_LOGIN"]);

                IConnectionFactory factory = new ConnectionFactory(url);

                _connection = factory.CreateConnection(_amqconfig["AMQC_LOGIN"],_amqconfig["AMQC_PASSWORD"]);
                _connection.Start();
                _session = _connection.CreateSession();

    // create destinations
                
                var subscriptionsq=_config["subscriptions:queues"];
                var subscriptionst=_config["subscriptions:topics"];

                _destinations.Clear();
                _consumers.Clear();

                if (subscriptionsq!=null)
                    foreach(string q in subscriptionsq.Split(","))
                    {
                        var qq="/queue/"+q;
                        if (!_destinations.ContainsKey(qq))
                            _destinations.Add(qq,_session.GetQueue(q));

                    }
                if (subscriptionst!=null)
                    foreach(string t in subscriptionst.Split(","))
                    {
                        var tt="/topic/"+t;
                        if (!_destinations.ContainsKey(tt))
                            _destinations.Add(tt,_session.GetTopic(t));

                    }      

                _connected=true;

                foreach(IDestination dest in _destinations.Values)
                {
                    IMessageConsumer consumer = _session.CreateConsumer(dest);
                    _consumers.Add(consumer);
                }
            }
        }
        public void SendMessage(string aQueue,string aMessage)
        {
            SendMessage(aQueue,aMessage,null);
        }
        public void SendMessage(string aQueue,string aMessage,Dictionary<string,object> aHeaders)
        {
            if(!_connected)
            {
                log.Info("Not connected");    
                return;
            }
            try
            {   
                lock(_lock)
                {         
                    log.Info(">SENDING message to "+aQueue);
                    IDestination dest;
                    if(aQueue.IndexOf("/queue")>=0)
                        dest = _session.GetQueue(aQueue.Split("/")[2]);
                    else
                        dest = _session.GetTopic(aQueue.Split("/")[2]);

                    using (IMessageProducer producer = _session.CreateProducer(dest))
                    {                
                        var textMessage = producer.CreateTextMessage(aMessage);
                        
                        var map=textMessage.Properties;
                        map.Clear();
                        if (aHeaders==null)
                            aHeaders=new Dictionary<string, object>();
                        
                        aHeaders["Emitter"]=System.Reflection.Assembly.GetExecutingAssembly().GetName().Name;
                        
                        foreach (System.Collections.Generic.KeyValuePair
                                <string, object> kvp in aHeaders)                
                            map[kvp.Key] = kvp.Value;
                                    
                        producer.Send(textMessage);
                    }
                }
            }
            catch(Exception e)
            {
                log.Error("Unable to send message");
                Disconnect();
            }

        }
    }
}
