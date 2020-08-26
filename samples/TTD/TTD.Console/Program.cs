﻿using Dapr.Client;
using Microsoft.Extensions.Logging;
using Fiffi;
using Dapr.EventStore;
using System.Text.Json;
using System;
using System.Net;
using EventStore.Client;

namespace TTD.Console
{
    class Program
    {
        static void Main(string[] args)
        {
            //var jsonOptions = new JsonSerializerOptions()
            //{
            //    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            //    PropertyNameCaseInsensitive = true
            //};

            //var client = new DaprClientBuilder()
            //    .UseJsonSerializationOptions(jsonOptions)
            //    .Build();

            //var loggerFactory = new LoggerFactory();
            //var logger = loggerFactory.CreateLogger<DaprEventStore>();

            //var store = new Fiffi.Dapr.DaprEventStore(new DaprEventStore(client, logger), TypeResolver.FromMap(TypeResolver.GetEventsInAssembly<Arrived>()))o;
            var tr = TypeResolver.FromMap(TypeResolver.GetEventsInAssembly<Arrived>());
            var settings = new EventStoreClientSettings
            {
                ConnectivitySettings = {
                Address = new Uri("https://localhost:2113")
            },
                DefaultCredentials = new UserCredentials("admin", "changeit")
            };

            var client = new EventStoreClient(settings);

            var (time, _) = TTD.Fiffied.App.RunAsync(new Fiffi.EventStoreDB.EventStore(client, tr), args).GetAwaiter().GetResult();
        }
    }
}
