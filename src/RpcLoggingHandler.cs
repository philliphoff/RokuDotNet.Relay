using System;
using System.Threading;
using System.Threading.Tasks;
using RokuDotNet.Proxy;
using Serilog;

namespace RokuDotNet.Relay
{
    internal sealed class RpcLoggingHandler : IRokuRpcServerHandler
    {
        private readonly IRokuRpcServerHandler innerHandler;
        private readonly ILogger logger;

        public RpcLoggingHandler(IRokuRpcServerHandler innerHandler, ILogger logger)
        {
            this.innerHandler = innerHandler ?? throw new ArgumentNullException(nameof(innerHandler));
            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        #region IRokuRpcServerHandler Members

        public async Task<MethodInvocationResponse> HandleMethodInvocationAsync(MethodInvocation invocation, CancellationToken cancellationToken = default)
        {
            this.logger.Information("Received RPC {MethodName}", invocation.MethodName);

            var response = await this.innerHandler.HandleMethodInvocationAsync(invocation, cancellationToken).ConfigureAwait(false);

            this.logger.Information("Sending RPC {Payload}", response.Payload);

            return response;
        }

        #endregion
    }
}