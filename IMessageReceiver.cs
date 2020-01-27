using System.Collections.Generic;

namespace NYX_Skeleton
{
    public interface IMessageReceiver
    {
        public void MessageReceived(string aTopic,string aMessage,Dictionary<string,object> aHeaders);
    }
}