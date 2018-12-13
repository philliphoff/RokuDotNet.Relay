using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Serilog;
using Serilog.Events;

namespace RokuDotNet.Relay
{
    internal sealed class HttpLoggingHandler : DelegatingHandler
    {
        private readonly ILogger logger;

        public HttpLoggingHandler(HttpMessageHandler innerHandler, ILogger logger)
            : base(innerHandler)
        {
            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            this.logger.Information("Sending HTTP {Method} {Uri}", request.Method, request.RequestUri);

            var response = await base.SendAsync(request, cancellationToken).ConfigureAwait(false);

            this.logger.Write(
                response.IsSuccessStatusCode ? LogEventLevel.Information : LogEventLevel.Error,
                "Received HTTP {StatusCode} {Message}",
                (int)response.StatusCode,
                response.ReasonPhrase);

            return response;
        }
    }
}