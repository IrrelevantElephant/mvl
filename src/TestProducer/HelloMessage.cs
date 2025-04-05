using MVL.Core;

namespace TestProducer;

internal class HelloMessage : IMessage
{
    public string Greeting { get; set; }
}