using System.Net;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using SimpleConnect.Functions.Services;

namespace SimpleConnect.Functions.Functions;

public class ShortLinkFunction
{
    private readonly IRoomStoreService roomStore;

    public ShortLinkFunction(IRoomStoreService roomStore)
    {
        this.roomStore = roomStore;
    }

    [Function("ResolveShortLink")]
    public async Task<HttpResponseData> Resolve(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "s/{code}")] HttpRequestData req,
        string code)
    {
        var inviteUrl = await this.roomStore.GetShortLinkUrlAsync(code);

        if (inviteUrl == null)
        {
            return req.CreateResponse(HttpStatusCode.NotFound);
        }

        var response = req.CreateResponse(HttpStatusCode.Redirect);
        response.Headers.Add("Location", inviteUrl);
        return response;
    }
}
