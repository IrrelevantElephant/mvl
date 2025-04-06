namespace MVL.Core;

public interface IConsumer<T> where T : IMessage
{
    public Task ConsumeAsync(T message, CancellationToken cancellationToken);
}