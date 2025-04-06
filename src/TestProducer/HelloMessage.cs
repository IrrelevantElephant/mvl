using MVL.Core;

namespace TestProducer;

internal class HelloMessage : IMessage
{
    public required string Greeting { get; set; }
}