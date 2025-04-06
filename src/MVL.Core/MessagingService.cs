using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Reflection;
using System.Text;
using System.Text.Json;

namespace MVL.Core;

internal class MessagingService : IHostedService
{
    private readonly MvlSettings _mvlSettings;
    private readonly ILogger<MessagingService> _logger;
    private readonly IChannel _channel;

    public MessagingService(MvlSettings mvlSettings, ILogger<MessagingService> logger, IChannel channel)
    {
        _mvlSettings = mvlSettings;
        _logger = logger;
        _channel = channel;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Starting service");

        var consumers = GetConsumers();

        var consumerDeclarations = new List<Task<string>>();

        foreach (var consumer in consumers)
        {
            var basicConsumer = new AsyncEventingBasicConsumer(_channel);
            basicConsumer.ReceivedAsync += async (model, ea) =>
            {
                var body = ea.Body.ToArray();
                var message = Encoding.UTF8.GetString(body);

                var messageDeserialised = JsonSerializer.Deserialize(message, consumer.MessageType);

                var casted = (IMessage)Convert.ChangeType(messageDeserialised, consumer.MessageType);

                var handler = Activator.CreateInstance(consumer.ConsumerType);

                var methodInfo = consumer.ConsumerType.GetMethod(nameof(IConsumer<IMessage>.ConsumeAsync));
                
                var argumentsList = new object[] { casted, cancellationToken };

                methodInfo.Invoke(handler, argumentsList);
            };

            consumerDeclarations.Add(_channel.BasicConsumeAsync(consumer.ConsumerType.FullName, autoAck: true, consumer: basicConsumer, cancellationToken: cancellationToken));
        }

        await Task.WhenAll(consumerDeclarations);
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Shutting down service");

        await _channel.CloseAsync(cancellationToken);
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