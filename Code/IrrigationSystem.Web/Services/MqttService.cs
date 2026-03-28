using MQTTnet;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Configuration;

namespace IrrigationSystem.Web.Services;

public class MqttService
{
    private readonly IMqttClient MqttClient;
    private readonly ILogger<MqttService> Logger;
    private readonly IConfiguration Configuration;

    public MqttService(ILogger<MqttService> logger, IConfiguration configuration)
    {
        Logger = logger;
        Configuration = configuration;
        var factory = new MqttClientFactory();
        MqttClient = factory.CreateMqttClient();
        
        Task.Run(async () => await ConnectAsync());
    }

    private async Task ConnectAsync()
    {
        try
        {
            var brokerHost = Configuration.GetValue<string>("Mqtt:BrokerHost") ?? "localhost";
            var brokerPort = Configuration.GetValue<int>("Mqtt:BrokerPort", 1883);

            var options = new MqttClientOptionsBuilder()
                .WithTcpServer(brokerHost, brokerPort)
                .WithClientId("IrrigationWeb")
                .Build();

            await MqttClient.ConnectAsync(options);

            // World-friendly logging: only debug when connected, avoid noisy info if not required.
            Logger.LogDebug("MQTT client connected.");
        }
        catch (Exception ex)
        {
            // Avoid spam in console. Keep log level debug or warning only.
            Logger.LogDebug(ex, "MQTT broker connection failed; running in degraded mode without MQTT.");
        }
    }

    public async Task<bool> OpenValveAsync(int zoneId, int durationSeconds)
    {
        try
        {
            if (MqttClient == null || !MqttClient.IsConnected)
            {
                Logger.LogDebug("MQTT not connected -> open valve for zone {ZoneId} skipped (no-error mode)", zoneId);
                return false;
            }

            var command = new { action = "open", duration = durationSeconds };
            var payload = JsonSerializer.Serialize(command);
            
            var message = new MqttApplicationMessageBuilder()
                .WithTopic($"irrigation/zone/{zoneId}/valve")
                .WithPayload(payload)
                .Build();

            await MqttClient.PublishAsync(message);
            Logger.LogDebug("Sent valve open command to zone {ZoneId} for {Duration}s", zoneId, durationSeconds);
            return true;
        }
        catch (Exception ex)
        {
            Logger.LogDebug(ex, "MQTT publish open valve failed for zone {ZoneId}, degraded mode continuing", zoneId);
            return false;
        }
    }

    public async Task<bool> CloseValveAsync(int zoneId)
    {
        try
        {
            if (MqttClient == null || !MqttClient.IsConnected)
            {
                Logger.LogDebug("MQTT not connected -> close valve for zone {ZoneId} skipped (no-error mode)", zoneId);
                return false;
            }

            var command = new { action = "close" };
            var payload = JsonSerializer.Serialize(command);
            
            var message = new MqttApplicationMessageBuilder()
                .WithTopic($"irrigation/zone/{zoneId}/valve")
                .WithPayload(payload)
                .Build();

            await MqttClient.PublishAsync(message);
            Logger.LogDebug("Sent valve close command to zone {ZoneId}", zoneId);
            return true;
        }
        catch (Exception ex)
        {
            Logger.LogDebug(ex, "MQTT publish close valve failed for zone {ZoneId}, degraded mode continuing", zoneId);
            return false;
        }
    }
}