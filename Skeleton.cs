using System.Collections.Generic;

namespace NYX_Skeleton
{
    public class Skeleton : SkeletonBase
    {
        private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        static void Main(string[] args)
        {
            var prog = new Skeleton();
        }

        public Skeleton() : base()
        {
            log.Info("APP CONSTRUCTOR");
            log.Info(Configuration);
        }

        public override void Running()
        {
            log.Info("APP Running");
            SendMessage("/queue/COUCOU", "TOTOTOT");
        }

        public override void MessageReceived(string aTopic,
                                    string aMessage,
                                    Dictionary<string, object> aHeaders)
        {
            log.Info("APP Message Received..............");
            log.Info(aMessage);                        
        }
    }
}
