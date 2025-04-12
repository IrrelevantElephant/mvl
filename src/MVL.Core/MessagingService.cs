using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Reflection;
using System.Text;
using System.Text.Json;

namespace MVL.Core;

internal class MessagingService : IHostedService
{
    private readonly IOptions<MvlOptions> _options;
    private readonly ILogger<MessagingService> _logger;
    private readonly IServiceProvider _serviceProvider;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IChannel _channel;

    public MessagingService(IOptions<MvlOptions> options, ILogger<MessagingService> logger, IChannel channel, IServiceProvider serviceProvider, IServiceScopeFactory scopeFactory)
    {
        _options = options;
        _logger = logger;
        _channel = channel;
        _serviceProvider = serviceProvider;
        _scopeFactory = scopeFactory;
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

                using var scope = _scopeFactory.CreateScope();

                var consumerContext =
                    Activator.CreateInstance(
                        typeof(ConsumerContext<,>).MakeGenericType(consumer.ConsumerType, consumer.MessageType));

                if (consumerContext is IConsumerContext context)
                {
                    await context.Consume(_serviceProvider, casted, cancellationToken);
                }
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

public interface IConsumerContext
{
    Task Consume(IServiceProvider services, object message, CancellationToken cancellationToken);
}

public class ConsumerContext<TConsumer, TMessage> : IConsumerContext
    where TMessage : IMessage
    where TConsumer: IConsumer<TMessage>
{
    public async Task Consume(IServiceProvider services, object message, CancellationToken cancellationToken)
    {
        var consumer = services.GetRequiredService<TConsumer>();

        if (message is TMessage messageCasted)
        {
            await consumer.ConsumeAsync(messageCasted, cancellationToken);
        }
    }
}