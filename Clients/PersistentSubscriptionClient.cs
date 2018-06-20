using System;
using System.Net;
using System.Text;
using EventStore.ClientAPI;
using EventStore.ClientAPI.SystemData;
using Newtonsoft.Json;

namespace EventStorePersistentSubscriber.Clients
{
    public class PersistentSubscriptionClient
    {

        //Code was refactored here to resemble the likes in this article as it was easier to read and understand
        //https://codeopinion.com/event-store-persistent-subscriptions-demo/

        private IEventStoreConnection _conn;

        //setup persistent subscription on the web admin
        private static string STREAM = "$ce-Customer";
        private static string GROUP = "Customer";

        public static IPAddress ES_IP = IPAddress.Parse("127.0.0.1");
        private const int ES_PORT = 1112;

        private static readonly UserCredentials User = new UserCredentials("admin", "changeit");
        private EventStorePersistentSubscriptionBase _subscription;

        public void Start()
        {
            var settings = ConnectionSettings
             .Create()
             .KeepReconnecting()
             .KeepRetrying()
             //.EnableVerboseLogging()
             .UseConsoleLogger();

            using (_conn = EventStoreConnection.Create(settings, new IPEndPoint(ES_IP, ES_PORT)))
            {
                _conn.ConnectAsync().Wait();

                //CreateSubscription();
                ConnectToSubscription();

                //important - this needs to be INSIDE the using
                while (true)
                {
                    System.Threading.Thread.Sleep(100);
                }
            }
        }

        private void ConnectToSubscription()
        {
            var bufferSize = 10;
            var autoAck = true;

            Console.WriteLine("**** Connecting to subscription ...");

            _subscription = _conn.ConnectToPersistentSubscription(STREAM, GROUP,
                (_base, _event) => { EventAppeared(_base, _event); },
                (_base, _reason, _exception) => { SubscriptionDropped(_base, _reason, _exception); },
                User, bufferSize, autoAck);
        }

        private void SubscriptionDropped(EventStorePersistentSubscriptionBase eventStorePersistentSubscriptionBase, SubscriptionDropReason subscriptionDropReason, Exception ex)
        {
            Console.WriteLine($"**** Connection dropped reason? '{subscriptionDropReason}' exception? '{ex.Message}'- Reconnecting ...");
            ConnectToSubscription();
        }

        private static void EventAppeared(EventStorePersistentSubscriptionBase eventStorePersistentSubscriptionBase, ResolvedEvent resolvedEvent)
        {
            var x = resolvedEvent;

            Console.WriteLine($"{x.Event.Created.ToString("HH:mm:ss.ffff")} #{x.Event.EventNumber} -> {x.Event.EventType}' StreamID '{x.Event.EventStreamId}");

            var dataJSON = Encoding.ASCII.GetString(x.Event.Data);
            var metaJSON = Encoding.ASCII.GetString(x.Event.Metadata);

            //this needs to be a write domain deserialise
            Console.WriteLine("");
            Console.WriteLine($"EventType = {x.Event.EventType}");

            Console.WriteLine("");
            Console.WriteLine("DATA");
            Console.WriteLine(dataJSON);

            Console.WriteLine("");
            Console.WriteLine("META");
            Console.WriteLine(metaJSON);

            switch (x.Event.EventType)
            {
                case var exp when (exp.Contains("EventModel")):

                    dynamic metaData = JsonConvert.DeserializeObject(metaJSON);
                    var eventModel = JsonConvert.DeserializeObject<EventModel>(metaJSON);

                    break;
                default:
                    break;
            }

        }

        /*
        * Normally the creating of the subscription group is not done in your general executable code. 
        * Instead it is normally done as a step during an install or as an admin task when setting 
        * things up. You should assume the subscription exists in your code.
        */
        private void CreateSubscription()
        {
            PersistentSubscriptionSettings settings = PersistentSubscriptionSettings.Create()
                .DoNotResolveLinkTos()
                .StartFromCurrent();

            try
            {
                _conn.CreatePersistentSubscriptionAsync(STREAM, GROUP, settings, User).Wait();
            }
            catch (AggregateException ex)
            {
                if (ex.InnerException.GetType() != typeof(InvalidOperationException)
                    && ex.InnerException?.Message != $"Subscription group {GROUP} on stream {STREAM} already exists")
                {
                    throw;
                }
            }
        }

    }

    public class EventModel
    {
        public Guid Id { get; set; }
    }
}
