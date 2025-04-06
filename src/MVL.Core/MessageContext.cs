using RabbitMQ.Client;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;

namespace MVL.Core;

public class MessageContext : IMessageContext
{
    private readonly MvlSettings _settings;
    private IChannel? _channel;

    public MessageContext(MvlSettings settings)
    {
        _settings = settings;
    }

    public async Task PublishAsync(IMessage message, CancellationToken cancellationToken)
    {
        await EnsureChannelCreated();
        var fullName = message.GetType().FullName!;

        var json = JsonSerializer.Serialize<object>(message);
        var body = Encoding.UTF8.GetBytes(json);

        var properties = new BasicProperties();
        properties.Headers = new Dictionary<string, object>();
        properties.Headers.Add("Type", fullName);

        await _channel!.BasicPublishAsync(exchange: fullName, routingKey: string.Empty, true, basicProperties: properties, body: body, cancellationToken: cancellationToken);
    }

    private async Task EnsureChannelCreated()
    {
        if (_channel != null)
            return;

        var hostName = _settings.ConnectionString;

        var factory = new ConnectionFactory { HostName = hostName };
        var connection = await factory.CreateConnectionAsync();
        var channel = await connection.CreateChannelAsync();

        _channel = channel;
    }
}
