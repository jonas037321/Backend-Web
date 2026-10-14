using System.Net.Http.Headers;

namespace Api.Services;

// Alle Aufrufe an die Polar AccessLink API laufen über diese Klasse (BaseAddress: https://www.polaraccesslink.com/v3/)
public class PolarClient(HttpClient httpClient)
{
    public Task<HttpResponseMessage> SendAsync(HttpMethod method, string path, string accessToken, HttpContent? content = null)
    {
        var request = new HttpRequestMessage(method, path) { Content = content };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        return httpClient.SendAsync(request);
    }
}
