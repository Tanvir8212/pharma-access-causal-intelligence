using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(4));
if (args.Length != 1 || !Uri.TryCreate(args[0], UriKind.Absolute, out var endpoint)) return 2;
try
{
    using var client = new HttpClient();
    using var response = await client.GetAsync(endpoint, timeout.Token);
    return response.IsSuccessStatusCode ? 0 : 1;
}
catch (Exception error) when (error is HttpRequestException or OperationCanceledException)
{
    return 1;
}
