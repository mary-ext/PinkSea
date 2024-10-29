using System.Net.Http.Json;
using System.Text.Json;
using System.Web;
using PinkSea.AtProto.Http;
using PinkSea.AtProto.Models.OAuth;
using PinkSea.AtProto.OAuth;

namespace PinkSea.AtProto.Xrpc;

/// <summary>
/// An XRPC client.
/// </summary>
public class XrpcClient(
    DpopHttpClient client,
    OAuthState clientState)
    : IXrpcClient
{
    /// <inheritdoc />
    public async Task<TResponse?> Query<TResponse>(
        string nsid,
        object? parameters = null)
    {
        var actualEndpoint = $"{clientState.Pds}/xrpc/{nsid}";
        if (parameters is not null)
            actualEndpoint += $"?{ObjectToQueryParams(parameters)}";

        var resp = await client.Get(actualEndpoint, clientState.KeyPair);
        if (resp.IsSuccessStatusCode)
        {
            var str = await resp.Content.ReadAsStringAsync();
            Console.WriteLine($"Got back data from the PDS: {str}");
            return JsonSerializer.Deserialize<TResponse>(str);
        }

        return default;
    }

    /// <inheritdoc />
    public async Task<TResponse?> Procedure<TResponse>(
        string nsid,
        object? parameters = null)
    {
        var actualEndpoint = $"{clientState.Pds}/xrpc/{nsid}";
        var resp = await client.Post(actualEndpoint, parameters, clientState.KeyPair);
        
        if (resp.IsSuccessStatusCode)
        {
            var str = await resp.Content.ReadAsStringAsync();
            Console.WriteLine($"Got back data from the PDS: {str}");
            return JsonSerializer.Deserialize<TResponse>(str);
        }

        return default;
    }

    /// <summary>
    /// Converts an object to a query string.
    /// </summary>
    /// <param name="obj">The object.</param>
    /// <returns>The resulting query string.</returns>
    private string ObjectToQueryParams(object obj)
    {
        var props = from p in obj.GetType().GetProperties()
            where p.GetValue(obj, null) != null
            select p.Name.ToLowerInvariant() + "=" + HttpUtility.UrlEncode(p.GetValue(obj, null).ToString());

        return string.Join('&', props.ToArray());
    }

    /// <inheritdoc />
    public void Dispose()
    {
        client.Dispose();
    }
}