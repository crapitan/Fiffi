﻿using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Fiffi.CosmosChangeFeed
{
    public static class Extensions
    {
        public static async Task<ChangeFeedProcessor> CreateProcessorAsync<T>(
            this CosmosClient client,
            string databaseId,
            string leasesContainerId,
            string containerId,
            string processorName,
            string instanceName,
            Func<IReadOnlyCollection<T>, CancellationToken, Task> f)
        {
            var db = client.GetDatabase(databaseId);
            _ = await db.CreateContainerIfNotExistsAsync(leasesContainerId, "/id");
            var leasesContainer = client.GetContainer(databaseId, leasesContainerId);
            var monitoredContainer = client.GetContainer(databaseId, containerId);
            return monitoredContainer
                .GetChangeFeedProcessorBuilder<T>(processorName, (x, ct) => f(x, ct))
                .WithInstanceName(instanceName)
                .WithLeaseContainer(leasesContainer)
                .WithPollInterval(TimeSpan.FromSeconds(1))
                .Build();
        }

        public static IServiceCollection AddChangeFeedSubscription<T>(
            this IServiceCollection sc,
            IConfiguration configuration,
            Action<SubscriptionOptions> options,
            Func<IServiceProvider, Func<IReadOnlyCollection<T>, CancellationToken ,Task>> f)
            => sc
            .Tap(x =>
                x.AddOptions<SubscriptionOptions>()
                .Bind(configuration)
                .Configure(options)
                .ValidateDataAnnotations())
            .AddTransient<Func<CosmosClient>>(sp => () => { 
                var opt = sp.GetRequiredService<IOptions<SubscriptionOptions>>().Value;
                return new CosmosClient(opt.ServiceUri.ToString(), opt.Key);
            })
            .AddSingleton<Func<CosmosClient, Task<ChangeFeedProcessor>>>(sp => async (client) =>
            {
                var opt = sp.GetRequiredService<IOptions<SubscriptionOptions>>().Value;
                return await client.CreateProcessorAsync(
                    opt.DatabaseName, 
                    "leases",
                    opt.ContainerId,
                    opt.ProcessorName,
                    opt.InstanceName,
                    f(sp)
                 );
            })
            .AddHostedService<ChangeFeedHostedService>();
    }
}
