namespace MVL.Core;

public interface IMessageContext
{
    Task PublishAsync(IMessage message, CancellationToken cancellationToken);
}