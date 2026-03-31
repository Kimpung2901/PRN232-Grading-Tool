using System.Net.Http;
using System.Net.Sockets;

namespace Runner.Pipeline;

public sealed class HealthCheckService
{
    private readonly HttpClient _httpClient = new(new HttpClientHandler
    {
        AllowAutoRedirect = false
    })
    {
        Timeout = TimeSpan.FromSeconds(5)
    };

    public async Task EnsureApiReadyAsync(int port, CancellationToken cancellationToken = default)
    {
        var targetUri = new Uri($"http://127.0.0.1:{port}");
        Exception? lastException = null;

        // 20s total wait time with 2s delay between attempts
        for (var attempt = 1; attempt <= 10; attempt++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                var tcpReady = await IsTcpPortOpenAsync("127.0.0.1", port, cancellationToken);
                if (!tcpReady)
                {
                    lastException = new SocketException((int)SocketError.ConnectionRefused);
                    await Task.Delay(TimeSpan.FromSeconds(2), cancellationToken);
                    continue;
                }

                using var response = await _httpClient.GetAsync(targetUri, cancellationToken);
                return;
            }
            catch (HttpRequestException) when (attempt < 10)
            {
                lastException = null;
            }
            catch (TaskCanceledException) when (attempt < 10)
            {
                lastException = null;
            }
            catch (Exception ex) when (attempt < 10)
            {
                lastException = ex;
            }

            await Task.Delay(TimeSpan.FromSeconds(2), cancellationToken);
        }

        var detail = lastException is null ? string.Empty : $" Last error: {lastException.Message}";
        throw new PipelineException("API_START_FAILED", $"API did not become ready on port {port}.{detail}");
    }

    private static async Task<bool> IsTcpPortOpenAsync(string host, int port, CancellationToken cancellationToken)
    {
        using var tcpClient = new TcpClient();

        try
        {
            await tcpClient.ConnectAsync(host, port, cancellationToken);
            return true;
        }
        catch (SocketException)
        {
            return false;
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch
        {
            return false;
        }
    }
}
