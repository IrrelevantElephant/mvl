using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using RabbitMQ.Client;

namespace MVL.Core;

// ReSharper disable once InconsistentNaming
public static class IHostApplicationBuilderExtensions
{
    public static IHostApplicationBuilder AddMvl(this IHostApplicationBuilder hostApplicationBuilder, MvlSettings settings)
    {
        var factory = new ConnectionFactory { HostName = "localhost" };
        using var connection = factory.CreateConnectionAsync().Result;
        using var channel = connection.CreateChannelAsync().Result;

        foreach (var queue in GetQueuesToCreate())
        {
            channel.QueueDeclareAsync(queue: queue, durable: false, exclusive: false, autoDelete: false,
                arguments: null);
        }

        hostApplicationBuilder.Services.AddHostedService<MessagingService>();
        hostApplicationBuilder.Services.AddSingleton(settings);
        return hostApplicationBuilder;
    }

    private static IEnumerable<string> GetQueuesToCreate()
    {
        var type = typeof(IMessage);
        var types = AppDomain.CurrentDomain.GetAssemblies()
            .SelectMany(s => s.GetTypes())
            .Where(p => type.IsAssignableFrom(p) && p.IsClass);

        return types.Select(t => t.Name);
    }
}