namespace Agriloco.Api.Security;

// The accessor resolves the current request even though HttpClientFactory pools handlers.
public sealed class MemberApiHandler(IHttpContextAccessor accessor, IConfiguration configuration) : DelegatingHandler
{
    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var expected = new Uri(configuration["Application:PublicBaseUrl"]!);
        if (request.RequestUri?.GetLeftPart(UriPartial.Authority) != expected.GetLeftPart(UriPartial.Authority))
            throw new InvalidOperationException("Internal member API requests must target the configured application origin.");
        if (accessor.HttpContext is { } context)
        {
            request.Headers.Remove("Cookie");
            request.Headers.TryAddWithoutValidation("Cookie", context.Request.Headers.Cookie.ToString());
            request.Headers.Remove("Origin");
            request.Headers.TryAddWithoutValidation("Origin", expected.GetLeftPart(UriPartial.Authority));
        }
        return base.SendAsync(request, cancellationToken);
    }
}
