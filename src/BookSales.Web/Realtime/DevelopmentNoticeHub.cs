using System.Net;
using Microsoft.AspNetCore.SignalR;

namespace BookSales.Web.Realtime;

public sealed class DevelopmentNoticeHub(IWebHostEnvironment environment) : Hub
{
    public override Task OnConnectedAsync()
    {
        var address = Context.GetHttpContext()?.Connection.RemoteIpAddress;
        if (!environment.IsDevelopment() || address is null || !IPAddress.IsLoopback(address))
        {
            Context.Abort();
            throw new HubException("Development probe is available on loopback only.");
        }
        return base.OnConnectedAsync();
    }
}
