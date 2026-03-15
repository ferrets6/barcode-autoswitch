using CefSharp;
using CefSharp.Handler;

namespace BarcodeAutoSwitch.Infrastructure;

/// <summary>
/// CefSharp request handler that fires <see cref="ServerError"/> when the
/// Adriatica Press server responds with HTTP 5xx on a top-level navigation.
/// </summary>
internal sealed class HttpErrorRequestHandler : RequestHandler
{
    /// <summary>Raised on the CefSharp handler thread with the HTTP status code.</summary>
    public event Action<int>? ServerError;

    protected override IResourceRequestHandler? GetResourceRequestHandler(
        IWebBrowser chromiumWebBrowser, IBrowser browser, IFrame frame,
        IRequest request, bool isNavigation, bool isDownload,
        string requestInitiator, ref bool disableDefaultHandling)
    {
        // Only monitor top-level page navigations to avoid noise from sub-resources
        if (isNavigation && frame.IsMain)
            return new ServerErrorResourceHandler(code => ServerError?.Invoke(code));

        return base.GetResourceRequestHandler(
            chromiumWebBrowser, browser, frame, request,
            isNavigation, isDownload, requestInitiator, ref disableDefaultHandling);
    }
}

file sealed class ServerErrorResourceHandler : ResourceRequestHandler
{
    private readonly Action<int> _onServerError;

    internal ServerErrorResourceHandler(Action<int> onServerError)
    {
        _onServerError = onServerError;
    }

    protected override bool OnResourceResponse(
        IWebBrowser chromiumWebBrowser, IBrowser browser, IFrame frame,
        IRequest request, IResponse response)
    {
        if (response.StatusCode >= 500)
            _onServerError(response.StatusCode);

        return false; // false = do not retry the request
    }
}
