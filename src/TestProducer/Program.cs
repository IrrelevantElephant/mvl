using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using MVL.Core;
using TestProducer;

var builder = Host.CreateApplicationBuilder(args);

builder.AddMvl(options =>
{
    options.ConnectionString = "localhost";
});

var host = builder.Build();

var context = host.Services.GetRequiredService<IMessageContext>();

await context.PublishAsync(new HelloMessage
{
    Greeting = "hello world"
}, CancellationToken.None);

host.Run();
