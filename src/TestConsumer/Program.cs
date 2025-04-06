using Microsoft.Extensions.Hosting;
using MVL.Core;

var builder = Host.CreateApplicationBuilder(args);

builder.AddMvl(new MvlSettings { ConnectionString = "localhost" });

var host = builder.Build();
host.Run();