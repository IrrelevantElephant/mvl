using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using RabbitMQ.Client;
using System.Reflection;

namespace MVL.Core;

// ReSharper disable once InconsistentNaming
public static class IHostApplicationBuilderExtensions
{
    public static IHostApplicationBuilder AddMvl(this IHostApplicationBuilder hostApplicationBuilder, Action<MvlOptions> mvlOptions)
    {
        var options = new MvlOptions();
        mvlOptions.Invoke(options);

        var factory = new ConnectionFactory { HostName = options.ConnectionString };
        var connection = factory.CreateConnectionAsync().Result;
        var channel = connection.CreateChannelAsync().Result;

        var exchanges = GetExchangesToCreate();
        foreach (var exchange in exchanges)
        {
            channel.ExchangeDeclareAsync(exchange, ExchangeType.Direct);
        }

        var consumers = GetConsumers();
        foreach (var consumer in consumers)
        {
            channel.QueueDeclareAsync(consumer.Item1.FullName, durable: false, exclusive: false, autoDelete: false, arguments: null);

            channel.QueueBindAsync(consumer.Item1.FullName, consumer.Item2.FullName, string.Empty);
        }

        hostApplicationBuilder.Services.AddHostedService<MessagingService>();
        hostApplicationBuilder.Services.Configure(mvlOptions);
        hostApplicationBuilder.Services.AddSingleton(channel);

        hostApplicationBuilder.Services.AddSingleton<IMessageContext, MessageContext>();

        return hostApplicationBuilder;
    }

    private static IEnumerable<string> GetExchangesToCreate()
    {
        var type = typeof(IMessage);
        var types = AppDomain.CurrentDomain.GetAssemblies()
            .SelectMany(s => s.GetTypes())
            .Where(p => type.IsAssignableFrom(p) && p is { IsClass: true, IsGenericType: false });

        return types.Select(t => t.FullName!);
    }

    public static IEnumerable<(Type ConsumerType, Type MessageType)> GetConsumers()
    {
        var types = AppDomain.CurrentDomain.GetAssemblies()
            .SelectMany(GetConsumersByAssembly);

        return types;
    }

    public static IEnumerable<(Type ConsumerType, Type MessageType)> GetConsumersByAssembly(Assembly assembly)
    {
        return assembly.GetTypes()
            .Where(t => t is { IsClass: true, IsAbstract: false })
            .SelectMany(t => t.GetInterfaces()
                .Where(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IConsumer<>))
                .Where(i => typeof(IMessage).IsAssignableFrom(i.GetGenericArguments()[0]))
                .Select(i => (ConsumerType: t, MessageType: i.GetGenericArguments()[0]))
            );
    }
}