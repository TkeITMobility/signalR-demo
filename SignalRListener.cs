using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace SignalRDemo;

internal class SignalRListener : IHostedService
{
    private readonly ILogger _logger;
    private readonly Settings _settings;
    private HubConnection? _connection;

    public SignalRListener(
        ILoggerFactory loggerFactory,
        Settings settings)
    {
        _logger = loggerFactory.CreateLogger<SignalRListener>();
        _settings = settings;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        var token = await GetTokenAsync(cancellationToken);
        _connection = new HubConnectionBuilder()
                                .WithUrl(
                                    _settings.ApiUrl,
                                    options =>
                                    {
                                        options.Headers.Add("Authorization", $"Bearer {token}");
                                        options.Headers.Add("Ocp-Apim-Subscription-Key", _settings.SubscriptionKey);
                                    })
                                .Build();
        _connection.On<TwinStateLight>(
                "ProcessRealtimeTwinEventMessage",
                t => _logger.LogInformation($"{DateTime.Now.ToString("HH:mm:ss")} Current floor: {t.CurrentFloor}, Status: {t.EquipmentStatus}"));

        _logger.LogInformation("Connecting to {0}...", _settings.ApiUrl);
        await _connection.StartAsync(cancellationToken);
        await _connection.InvokeAsync("SubscribeToDigitalTwinUpdatesAsync", new[] { _settings.DeviceId }, cancellationToken);

        _logger.LogInformation("Connected to SignalR (ID: {0})", _connection.ConnectionId);
        _logger.LogInformation("Listening for messages...");
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        if (_connection == null) return;

        await _connection.StopAsync(CancellationToken.None);
        _logger.LogInformation("Closed connection.");
    }

    private async Task<string> GetTokenAsync(CancellationToken cancellationToken)
    {
        var tokenRequest = new HttpRequestMessage(
            HttpMethod.Post,
            "https://maxcustomerportal.b2clogin.com/maxcustomerportal.onmicrosoft.com/B2C_1_ROPC/oauth2/v2.0/token");
            
        tokenRequest.Content = new StringContent(
            "client_id=74c72ab2-3ade-45fe-83e0-cd5b90704539" +
           $"&username={_settings.Username}" +
           $"&password={_settings.Password}" + 
           $"&scope={_settings.ApiScope}" +
            "&grant_type=password",
            Encoding.UTF8,
            "application/x-www-form-urlencoded");

        using var httpClient = new HttpClient();
        var response = await httpClient.SendAsync(tokenRequest, cancellationToken);
        var content = await response.Content.ReadAsStringAsync(cancellationToken);

        return JsonDocument.Parse(content).RootElement.GetProperty("access_token").GetString()!;
    }
}
