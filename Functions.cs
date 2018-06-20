using System;
using System.Net;
using System.Text;
using EventStore.ClientAPI;
using Microsoft.Azure.WebJobs;
using MongoDB.Driver;
using Newtonsoft.Json;

namespace EventStorePersistentSubscriber
{
    public class Functions
    {

        public static string STREAM = "";
        public static string GROUP = "";

        public static int DEFAULTPORT = 1112;
        public static IPAddress IP_LEI = IPAddress.Parse("127.0.0.1");
        static IMongoDatabase MongoDB { get; set; }


        [NoAutomaticTrigger]
        public static void ProcessMethod(Microsoft.Extensions.Logging.ILogger logger)
        {
            var settings = ConnectionSettings.Create().EnableVerboseLogging().UseConsoleLogger();

            InitMongoClient();

            using (var conn = EventStoreConnection.Create(settings, new IPEndPoint(IP_LEI, DEFAULTPORT)))
            {
                conn.ConnectAsync().Wait();

                //Normally the creating of the subscription group is not done in your general executable code. 
                //Instead it is normally done as a step during an install or as an admin task when setting 
                //things up. You should assume the subscription exists in your code.
                //CreateSubscription(conn);

                conn.ConnectToPersistentSubscription(STREAM, GROUP,
                    (_, x) =>
                    {
                        try
                        {
                            Console.WriteLine($"{x.Event.Created.ToString("HH:mm:ss.ffff")} #{x.Event.EventNumber} -> {x.Event.EventType}' StreamID '{x.Event.EventStreamId}");

                            var dataJSON = Encoding.ASCII.GetString(x.Event.Data);
                            var metaJSON = Encoding.ASCII.GetString(x.Event.Metadata);

                            Console.WriteLine("");
                            Console.WriteLine("EventType = " + x.Event.EventType);

                            Console.WriteLine("");
                            Console.WriteLine("DATA");
                            Console.WriteLine(dataJSON);

                            Console.WriteLine("");
                            Console.WriteLine("META");
                            Console.WriteLine(metaJSON);

                            switch (x.Event.EventType)
                            {
                                case var exp when (exp.Contains("MyEvent")):
                                    dynamic metaData = JsonConvert.DeserializeObject(metaJSON);

                                    var pr = JsonConvert.DeserializeObject<EventModel>(dataJSON);

                                    try
                                    {

                                        //MongoDB.GetCollection

                                    }
                                    catch (Exception mongoException)
                                    {
                                        Console.WriteLine($"MONGO EXCPETION: {mongoException.Message}");
                                        Console.WriteLine($"MONGO EXCPETION: {mongoException.StackTrace}");
                                    }

                                    //Console.WriteLine($"Saved '{pr.First} {pr.Last}' to ReadStore");
                                    break;
                                default:
                                    break;
                            }
                        }
                        catch (Exception ex)
                        {

                            Console.WriteLine($"GENERAL EXCEPTION: {ex.Message}");
                            Console.WriteLine($"GENERAL EXCEPTION: {ex.StackTrace}");

                            throw ex; //force down the webjob
                        }


                    });

                //Keep this IN the using to keep connection active
                while (true)
                {
                    System.Threading.Thread.Sleep(100);
                }
            }

        }

        private static void InitMongoClient()
        {
            var _client = new MongoClient(new MongoUrl("mongodb://"));
            MongoDB = _client.GetDatabase("example");
        }
    }

    public class EventModel
    {
        public Guid Id { get; set; }
    }
}

