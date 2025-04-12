using Microsoft.Extensions.Hosting;
using MVL.Core;

var builder = Host.CreateApplicationBuilder(args);

builder.AddMvl(options =>
{
    options.ConnectionString = "localhost";
});

var host = builder.Build();
host.Run();