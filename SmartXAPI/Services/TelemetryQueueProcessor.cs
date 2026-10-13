using Microsoft.Extensions.Hosting;
using System.Threading;
using System.Threading.Tasks;

namespace SmartXAPI.Services;

public class TelemetryQueueProcessor : BackgroundService
{
    private readonly TelemetryIngestionService _telemetryService;

    public TelemetryQueueProcessor(
        TelemetryIngestionService telemetryService)
    {
        _telemetryService = telemetryService;
    }

    protected override async Task ExecuteAsync(
        CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            _telemetryService.ProcessQueuedPackets();

            await Task.Delay(100, stoppingToken);
        }
    }
}