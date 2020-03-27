using System;
using System.Collections.Generic;
using EventStore.ClientAPI;
using EventStore.ClientAPI.SystemData;

namespace EventDrivenThinking.EventStoreCli
{
    class Program
    {

        static IEventStoreConnection Connect()
        {
            ConnectionSettings settings = ConnectionSettings.Create()
                .UseSslConnection(false)
                .SetDefaultUserCredentials(new UserCredentials("admin","changeit"));
            IEventStoreConnection connection = EventStoreConnection.Create(settings, new Uri("tcp://localhost:1113"));
            connection.ConnectAsync().GetAwaiter().GetResult();
            return connection;
        }

        static void Main(string[] args)
        {
            if (args[0] == "list" && args.Length == 2)
            {
                string streamName = args[1];

                var connection = Connect();
                var slices = connection.ReadStreamEventsForwardAsync(streamName, 0, 1000, true)
                    .GetAwaiter().GetResult();

                Dictionary<Guid, long> originalLocations = new Dictionary<Guid, long>();
                foreach (var i in slices.Events)
                {
                    int index = i.Event.EventStreamId.IndexOf('-');
                    string catalog = i.Event.EventStreamId.Remove(index);
                    string id = i.Event.EventStreamId.Substring(index + 1);

                    string comments = "";
                    if (!originalLocations.ContainsKey(i.Event.EventId))
                        originalLocations.Add(i.Event.EventId, i.Event.EventNumber);
                    else comments += $"\t# DUPLICATE({originalLocations[i.Event.EventId]})";
                    if (comments != string.Empty)
                        Console.ForegroundColor = ConsoleColor.Red;
                    
                    Console.WriteLine($"{i.OriginalEventNumber}\t{i.Event.EventNumber}\t{i.Event.Created}\t{catalog}\t{id}\t{i.Event.EventType} {comments}");
                    Console.ForegroundColor = ConsoleColor.White;
                }
            }
        }
    }
}
