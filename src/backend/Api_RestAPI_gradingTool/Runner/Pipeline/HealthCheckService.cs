using System.Net.Http;

namespace Runner.Pipeline;

public sealed class HealthCheckService
{
    private readonly HttpClient _httpClient = new()
    {
        Timeout = TimeSpan.FromSeconds(5)
    };

    public async Task EnsureApiReadyAsync(int port, CancellationToken cancellationToken = default)
    {
        var targetUri = new Uri($"http://localhost:{port}");

        for (var attempt = 1; attempt <= 10; attempt++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                using var response = await _httpClient.GetAsync(targetUri, cancellationToken);
                return;
            }
            catch (HttpRequestException) when (attempt < 10)
            {
            }
            catch (TaskCanceledException) when (attempt < 10)
            {
            }

            await Task.Delay(TimeSpan.FromSeconds(2), cancellationToken);
        }

        throw new PipelineException("API_START_FAILED", $"API did not become ready on port {port}.");
    }
}
