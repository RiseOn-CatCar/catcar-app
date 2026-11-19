namespace CatCar.Contexts.Communication.Infrastructure;

using CatCar.Contexts.Communication.Notifications;
using Microsoft.Extensions.Hosting;

internal sealed class EmailTransportStartup(IEmailSender sender) : IHostedService
{
    public Task StartAsync(CancellationToken cancellationToken)
    {
        _ = sender;
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
