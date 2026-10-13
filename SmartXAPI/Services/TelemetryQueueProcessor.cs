using Microsoft.Extensions.Hosting;
using System.Threading;
using System.Threading.Tasks;

namespace SmartXAPI.Services;

public class TelemetryQueueProcessor : BackgroundService
{
    private readonly TelemetryIngestionService _telemetryService;
    private readonly CriticalAlertService _criticalAlertService;

    public TelemetryQueueProcessor(
        TelemetryIngestionService telemetryService,
        CriticalAlertService criticalAlertService)
    {
        _telemetryService = telemetryService;
        _criticalAlertService = criticalAlertService;
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