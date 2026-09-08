using System;
using System.Net.Http;

namespace SmartXWPF.Services;

public class ApiService
{
    public HttpClient Client { get; }

    public ApiService(string baseUrl)
    {
        if (string.IsNullOrWhiteSpace(baseUrl))
        {
            throw new InvalidOperationException(
                "The Smart-X API base URL is not configured.");
        }

        Client = new HttpClient
        {
            BaseAddress = new Uri(baseUrl)
        };
    }
}