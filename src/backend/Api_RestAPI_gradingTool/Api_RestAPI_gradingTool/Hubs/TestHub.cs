using Api_RestAPI_gradingTool.Contracts.Grading;
using Microsoft.AspNetCore.SignalR;

namespace Api_RestAPI_gradingTool.Hubs;

public sealed class TestHub : Hub
{
    public Task BroadcastTestResult(TestResultRequest result)
    {
        return Clients.All.SendAsync("ReceiveTestResult", result);
    }
}
