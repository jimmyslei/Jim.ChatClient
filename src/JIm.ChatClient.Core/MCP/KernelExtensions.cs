using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using ModelContextProtocol.Client;
using ModelContextProtocol.Protocol.Transport;
using ModelContextProtocol;
using NPOI.XSSF.UserModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Concurrent;

namespace JIm.ChatClient.Core.MCP
{
    /// <summary>
    /// Extension methods for KernelPlugin
    /// </summary>
    public static class KernelExtensions
    {
        private static readonly ConcurrentDictionary<string, IKernelBuilderPlugins> SseMap = new();

        /// <summary>
            /// Creates a Model Content Protocol plugin from an SSE server that contains the specified MCP functions and adds it into the plugin collection.
            /// </summary>
            /// <param name="endpoint"></param>
            /// <param name="serverName"></param>
            /// <param name="cancellationToken">The optional <see cref="CancellationToken"/>.</param>
            /// <param name="plugins"></param>
            /// <returns>A <see cref="KernelPlugin"/> containing the functions.</returns>
        public static async Task<IKernelBuilderPlugins> AddMcpFunctionsFromSseServerAsync(this IKernelBuilderPlugins plugins,
       string endpoint, string serverName, CancellationToken cancellationToken = default)
        {
            var key = ToSafePluginName(serverName);

            if (SseMap.TryGetValue(key, out var sseKernelPlugin))
            {
                return sseKernelPlugin;
            }

            var mcpClient = await GetClientAsync(serverName, endpoint, null, null, cancellationToken).ConfigureAwait(false);
            var functions = await mcpClient.MapToFunctionsAsync(cancellationToken: cancellationToken).ConfigureAwait(false);

            cancellationToken.Register(() => mcpClient.DisposeAsync().ConfigureAwait(false).GetAwaiter().GetResult());

            sseKernelPlugin = plugins.AddFromFunctions(key, functions);
            return SseMap[key] = sseKernelPlugin;
        }

        private static async Task<IMcpClient> GetClientAsync(string serverName, string? endpoint,
           Dictionary<string, string>? transportOptions, ILoggerFactory? loggerFactory,
           CancellationToken cancellationToken)
        {
            var transportType = !string.IsNullOrEmpty(endpoint) ? TransportTypes.Sse : TransportTypes.StdIo;

            McpClientOptions options = new()
            {
                ClientInfo = new()
                {
                    Name = $"{serverName} {transportType}Client",
                    Version = "1.0.0"
                }
            };

            var config = new McpServerConfig
            {
                Id = serverName.ToLowerInvariant(),
                Name = serverName,
                Location = endpoint,
                TransportType = transportType,
                TransportOptions = transportOptions
            };

            return await McpClientFactory.CreateAsync(config, options,
               loggerFactory: loggerFactory ?? NullLoggerFactory.Instance, cancellationToken: cancellationToken);
        }

        // A plugin name can contain only ASCII letters, digits, and underscores.
        private static string ToSafePluginName(string serverName)
        {
            return Regex.Replace(serverName, @"[^\w]", "_");
        }
    }
}
