using MVL.Core;

namespace TestProducer;

internal class TestConsumer : IConsumer<HelloMessage>
{
    public Task ConsumeAsync(HelloMessage message, CancellationToken cancellationToken)
    {
        Console.WriteLine($"Greetings from a consumer {message.Greeting}");
        return Task.CompletedTask;
    }
}