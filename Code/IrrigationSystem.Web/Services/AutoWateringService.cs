using IrrigationSystem.Web.Models;
using Microsoft.Extensions.Configuration;

namespace IrrigationSystem.Web.Services;

public class AutoWateringService : BackgroundService
{
    private readonly ILogger<AutoWateringService> Logger;
    private readonly ISensorDataService DataService;
    private readonly MqttService MqttService;
    private readonly IConfiguration Configuration;
    private readonly int CheckIntervalSeconds;
    private readonly int MinHoursBetweenWaterings;

    public AutoWateringService(
        ILogger<AutoWateringService> logger,
        ISensorDataService dataService,
        MqttService mqttService,
        IConfiguration configuration)
    {
        Logger = logger;
        DataService = dataService;
        MqttService = mqttService;
        Configuration = configuration;
        CheckIntervalSeconds = Configuration.GetValue<int>("Irrigation:AutoWateringCheckIntervalSeconds", 300);
        MinHoursBetweenWaterings = Configuration.GetValue<int>("Irrigation:MinHoursBetweenWaterings", 2);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        Logger.LogInformation("Auto-watering service started - checking every {Interval} seconds", CheckIntervalSeconds);

        await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await CheckAndWaterZonesAsync();
                await Task.Delay(TimeSpan.FromSeconds(CheckIntervalSeconds), stoppingToken);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Error in auto-watering service");
                await Task.Delay(TimeSpan.FromSeconds(60), stoppingToken);
            }
        }
    }

    private async Task CheckAndWaterZonesAsync()
    {
        var settings = await DataService.GetSystemSettingsAsync();
        
        if (settings == null || !settings.AutoWateringEnabled)
        {
            Logger.LogInformation("Auto-watering is disabled in settings - skipping check");
            return;
        }

        if (settings.NightModeEnabled && !IsNightMode(settings))
        {
            Logger.LogInformation("Night mode enabled but not in watering hours - skipping");
            return;
        }

        var zones = await DataService.GetZonesAsync();
        var latestReadings = await DataService.GetLatestReadingsAsync();

        foreach (var zone in zones)
        {
            if (!zone.IsActive)
            {
                continue;
            }

            var reading = latestReadings.FirstOrDefault(r => r.ZoneId == zone.Id);

            if (reading == null || !reading.Moisture.HasValue)
            {
                Logger.LogWarning("Zone {ZoneId}: No recent sensor data, skipping auto-watering", zone.Id);
                continue;
            }

            if (reading.Moisture.Value < zone.MoistureThreshold)
            {
                var lastWatering = await GetLastWateringTimeAsync(zone.Id);
                var hoursSinceLastWatering = lastWatering.HasValue 
                    ? (DateTime.Now - lastWatering.Value).TotalHours 
                    : double.MaxValue;

                var minHours = MinHoursBetweenWaterings;
                if (settings.EcoModeEnabled)
                {
                    minHours = (int)(minHours * 1.5);
                    Logger.LogInformation("Zone {ZoneId}: Eco mode active - minimum wait time increased to {Hours}h", zone.Id, minHours);
                }

                if (hoursSinceLastWatering < minHours)
                {
                    Logger.LogInformation("Zone {ZoneId}: Moisture {Moisture}% below threshold {Threshold}%, but watered {Hours:F1}h ago - waiting", 
                        zone.Id, reading.Moisture.Value, zone.MoistureThreshold, hoursSinceLastWatering);
                    continue;
                }

                Logger.LogInformation("Zone {ZoneId}: Moisture {Moisture}% below threshold {Threshold}% - triggering auto-watering", 
                    zone.Id, reading.Moisture.Value, zone.MoistureThreshold);

                // MQTT broker is not used in this demo mode. Simulate/record event locally only.
                var eventId = await DataService.StartIrrigationEventAsync(zone.Id, "Auto", reading.Moisture.Value);
                await Task.Delay(settings.DefaultWateringDuration * 1000);
                await DataService.EndIrrigationEventAsync(eventId, settings.DefaultWateringDuration, reading.Moisture.Value);
            }
        }
    }

    private bool IsNightMode(SystemSettings settings)
    {
        var currentHour = DateTime.Now.Hour;
        
        if (settings.NightModeStartHour < settings.NightModeEndHour)
        {
            return currentHour >= settings.NightModeStartHour && currentHour < settings.NightModeEndHour;
        }
        else
        {
            return currentHour >= settings.NightModeStartHour || currentHour < settings.NightModeEndHour;
        }
    }

    private async Task<DateTime?> GetLastWateringTimeAsync(int zoneId)
    {
        var recentEvents = await DataService.GetRecentIrrigationEventsAsync(zoneId, 1);
        return recentEvents.FirstOrDefault()?.StartedAt;
    }

    private async Task WaterZoneAsync(int zoneId, float moistureBefore, int durationSeconds)
    {
        var eventId = await DataService.StartIrrigationEventAsync(zoneId, "Auto", moistureBefore);

        bool success = false;
        try
        {
            success = await MqttService.OpenValveAsync(zoneId, durationSeconds);
        }
        catch (Exception ex)
        {
            Logger.LogWarning(ex, "Zone {ZoneId}: Warning - failed to send open valve command, continuing in offline mode", zoneId);
        }

        if (!success)
        {
            Logger.LogWarning("Zone {ZoneId}: MQTT unreachable, falling back to local simulation for {Duration}s", zoneId, durationSeconds);
        }
        else
        {
            Logger.LogInformation("Zone {ZoneId}: Command sent to open valve for {Duration}s", zoneId, durationSeconds);
        }

        await DataService.LogSystemMessageAsync("INFO", 
            $"Auto-watering {(success ? "started" : "simulated")} for Zone {zoneId} (moisture: {moistureBefore:F1}%)");

        await Task.Delay(durationSeconds * 1000);

        if (success)
        {
            try
            {
                await MqttService.CloseValveAsync(zoneId);
            }
            catch (Exception ex)
            {
                Logger.LogWarning(ex, "Zone {ZoneId}: Warning - failed to send close valve command", zoneId);
            }
        }

        var newReadings = await DataService.GetLatestReadingsAsync();
        var newReading = newReadings.FirstOrDefault(r => r.ZoneId == zoneId);
        var moistureAfter = newReading?.Moisture;

        await DataService.EndIrrigationEventAsync(eventId, durationSeconds, moistureAfter);

        Logger.LogInformation("Zone {ZoneId}: Watering path completed - moisture before: {Before:F1}%, after: {After}", 
            zoneId, moistureBefore, moistureAfter.HasValue ? $"{moistureAfter.Value:F1}%" : "N/A");
    }
}