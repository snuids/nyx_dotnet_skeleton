using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Xml;
using Microsoft.Extensions.Configuration;  
using Newtonsoft.Json;

namespace NYX_Skeleton
{
    public class SkeletonBase :IMessageReceiver
    {
        public IConfiguration Configuration { get; }

        public AMQ _amq;

        private DateTime _last_life_sign=DateTime.UtcNow;

        private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        public SkeletonBase()
        {
// INIT LOG 4 NET
            XmlDocument log4netConfig = new XmlDocument();
            log4netConfig.Load(File.OpenRead("log4net.config"));
            var repo = log4net.LogManager.CreateRepository(Assembly.GetEntryAssembly(),
               typeof(log4net.Repository.Hierarchy.Hierarchy));
            log4net.Config.XmlConfigurator.Configure(repo, log4netConfig["log4net"]);

            log.Info("Application starting...");
            log.Info(Configuration);
            var builder = new ConfigurationBuilder()     
                    .AddJsonFile("appSettings.json");   
            Configuration = builder.Build(); 
            log.Info(Configuration);
            log.Info("=========================="); 
            log.Info(Configuration["app"]);              
            log.Info(Configuration["ConnectionStrings:DefaultConnection"]);    

            var amqconfig=new Dictionary<String,String>();

            amqconfig["AMQC_URL"]=Environment.GetEnvironmentVariable("AMQC_URL");
            amqconfig["AMQC_LOGIN"]=Environment.GetEnvironmentVariable("AMQC_LOGIN");
            amqconfig["AMQC_PASSWORD"]=Environment.GetEnvironmentVariable("AMQC_PASSWORD");
            amqconfig["AMQC_PORT"]=Environment.GetEnvironmentVariable("AMQC_PORT");
            
// CREATE ACTIVE MQ
            _amq=new AMQ(this,Configuration,amqconfig);

            while(true)
            {
                if (_last_life_sign.AddSeconds(5)<DateTime.UtcNow)
                {
                    _last_life_sign=DateTime.UtcNow;
                    log.Info("Send Life Sign");
                    

                    Dictionary<string, object> lifesign = new Dictionary<string, object>
                    {
                        {"error", "OK"},                        
                        {"type", "lifesign"},
                        {"eventtype", "lifesign"},
                        {"module", Configuration["app"]}            
                    };

                    var mes=JsonConvert.SerializeObject( lifesign );

                    SendMessage("/topic/NYX_MODULE_INFO",mes);

                }
                System.Threading.Thread.Sleep(1000);
                Running();
            }
        }            
        public virtual void Running()
        {
            log.Info("Base Running"); 
        }

        public virtual void MessageReceived(string aTopic,
                                    string aMessage,
                                    Dictionary<string, object> aHeaders)
        {
            log.Info("Base Message Received"); 
            log.Info(aMessage); 
        }

        public void SendMessage(string aTopic,
                                    string aMessage)
        {
            SendMessage(aTopic,aMessage,null); 
        }
        public void SendMessage(string aTopic,
                                    string aMessage,
                                    Dictionary<string, object> aHeaders)
        {
            _amq.SendMessage(aTopic,aMessage,aHeaders); 
        }
    }
}
