using System;
using System.Diagnostics;
using System.Globalization;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using NuGet.Common;
using NuGet.Protocol;

namespace Xenko.Core.Packages
{
    // Note: it's a copy of HttpRetryHandler from NuGet 4.6.0 (4.5.0 one doesn't copy headers which we need to know size).
    // Once we use 4.6.0, we could just delegate to existing one and rewrap.
    class NugetHttpRetryHandlerWithDownloadProgress : IHttpRetryHandler
    {
        private INugetDownloadProgress downloadProgress;

        public NugetHttpRetryHandlerWithDownloadProgress(INugetDownloadProgress downloadProgress)
        {
            this.downloadProgress = downloadProgress;
        }

        /// <summary>
        /// Make an HTTP request while retrying after failed attempts or timeouts.
        /// </summary>
        /// <remarks>
        /// This method accepts a factory to create instances of the <see cref="HttpRequestMessage"/> because
        /// requests cannot always be used. For example, suppose the request is a POST and contains content
        /// of a stream that can only be consumed once.
        /// </remarks>
        public async Task<HttpResponseMessage> SendAsync(
            HttpRetryHandlerRequest request,
            ILogger log,
            CancellationToken cancellationToken)
        {
            var tries = 0;
            HttpResponseMessage response = null;
            var success = false;

            while (tries < request.MaxTries && !success)
            {
                if (tries > 0)
                {
                    await Task.Delay(request.RetryDelay, cancellationToken);
                }

                tries++;
                success = true;

                using (var requestMessage = request.RequestFactory())
                {
                    var stopwatch = Stopwatch.StartNew();
                    var requestUri = requestMessage.RequestUri.ToString();

                    try
                    {
                        // The only time that we will be disposing this existing response is if we have 
                        // successfully fetched an HTTP response but the response has an status code indicating
                        // failure (i.e. HTTP status code >= 500).
                        // 
                        // If we don't even get an HTTP response message because an exception is thrown, then there
                        // is no response instance to dispose. Additionally, we cannot use a finally here because
                        // the caller needs the response instance returned in a non-disposed state.
                        //
                        // Also, remember that if an HTTP server continuously returns a failure status code (like
                        // 500 Internal Server Error), we will retry some number of times but eventually return the
                        // response as-is, expecting the caller to check the status code as well. This results in the
                        // success variable being set to false but the response being returned to the caller without
                        // disposing it.
                        response?.Dispose();

                        log.LogInformation("  " + string.Format(
                                               CultureInfo.InvariantCulture,
                                               "{0} {1}",
                                               requestMessage.Method,
                                               requestUri));

                        // Issue the request.
                        var timeoutMessage = string.Format(
                            CultureInfo.CurrentCulture,
                            "The HTTP request to '{0} {1}' has timed out after {2}ms.",
                            requestMessage.Method,
                            requestUri,
                            (int)request.RequestTimeout.TotalMilliseconds);
                        response = await TimeoutUtility.StartWithTimeout(
                            timeoutToken => request.HttpClient.SendAsync(requestMessage, request.CompletionOption, timeoutToken),
                            request.RequestTimeout,
                            timeoutMessage,
                            cancellationToken);

                        // Wrap the response stream so that the download can timeout.
                        if (response.Content != null)
                        {
                            var networkStream = await response.Content.ReadAsStreamAsync();

                            var contentLength = response.Content.Headers.ContentLength;
                            // Only bother to report if size is big enough (1MB)
                            if (contentLength != null && contentLength.Value >= 1024 * 1024)
                                networkStream = new NugetDownloadProgressStream(networkStream, contentLength.Value, downloadProgress);

                            var newContent = new DownloadTimeoutStreamContent(
                                requestUri,
                                networkStream,
                                request.DownloadTimeout);

                            // Copy over the content headers since we are replacing the HttpContent instance associated
                            // with the response message.
                            foreach (var header in response.Content.Headers)
                            {
                                newContent.Headers.TryAddWithoutValidation(header.Key, header.Value);
                            }

                            response.Content = newContent;
                        }

                        log.LogInformation("  " + string.Format(
                                               CultureInfo.InvariantCulture,
                                               "{0} {1} {2}ms",
                                               response.StatusCode,
                                               requestUri,
                                               stopwatch.ElapsedMilliseconds));

                        if ((int)response.StatusCode >= 500)
                        {
                            success = false;
                        }
                    }
                    catch (OperationCanceledException)
                    {
                        response?.Dispose();

                        throw;
                    }
                    catch (Exception e)
                    {
                        success = false;

                        response?.Dispose();

                        if (tries >= request.MaxTries)
                        {
                            throw;
                        }

                        log.LogInformation(string.Format(
                                               CultureInfo.CurrentCulture,
                                               "An error was encountered when fetching '{0} {1}'. The request will now be retried.",
                                               requestMessage.Method,
                                               requestUri,
                                               requestMessage)
                                           + Environment.NewLine
                                           + ExceptionUtilities.DisplayMessage(e));
                    }
                }
            }

            return response;
        }
    }
}
