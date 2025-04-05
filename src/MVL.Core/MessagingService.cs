using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace MVL.Core;

internal class MessagingService : IHostedService
{
    private readonly MvlSettings _mvlSettings;
    private readonly ILogger<MessagingService> _logger;


    public MessagingService(MvlSettings mvlSettings, ILogger<MessagingService> logger)
    {
        _mvlSettings = mvlSettings;
        _logger = logger;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Starting service");
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Shutting down service");

        return Task.CompletedTask;
    }
}