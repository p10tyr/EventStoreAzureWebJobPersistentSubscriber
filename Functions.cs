using Microsoft.Azure.WebJobs;

namespace EventStorePersistentSubscriber
{
    public class Functions
    {
        [NoAutomaticTrigger]
        public static void ProcessMethod(Microsoft.Extensions.Logging.ILogger logger)
        {
            var subscription = new Clients.PersistentSubscriptionClient();
            subscription.Start();
        }
    }
}