using IrrigationSystem.Web.Models;

namespace IrrigationSystem.Web.Services;

public interface ISensorDataService
{
    Task<List<Zone>> GetZonesAsync();
    Task<List<SensorReading>> GetLatestReadingsAsync();
    Task<List<SensorReading>> GetZoneHistoryAsync(int zoneId, int hours = 24);
    double CalculateMoisturePercentage(int adcValue);
    Task InsertSensorReadingAsync(int zoneId, float moisture, float temperature, float humidity);
    Task<List<SystemLog>> GetRecentLogsAsync(int count = 20);
    Task<List<IrrigationEvent>> GetRecentIrrigationEventsAsync(int zoneId, int count);
    Task<bool> UpdateZoneAsync(int zoneId, string name, string? plantType, float moistureThreshold, bool isActive);
    Task<int> StartIrrigationEventAsync(int zoneId, string triggerReason, float? moistureBefore);
    Task EndIrrigationEventAsync(int eventId, int durationSec, float? moistureAfter);
    Task LogSystemMessageAsync(string level, string message);
    Task<SystemSettings?> GetSystemSettingsAsync();
    Task<bool> UpdateSystemSettingsAsync(SystemSettings settings);
    Task<DisplaySettings?> GetDisplaySettingsAsync();
    Task<int> CleanOldDataAsync(int daysToKeep);
}