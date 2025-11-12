using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using Semantic.Kernel.Agent2AgentProtocol.Example.Core.Messaging;

namespace Semantic.Kernel.Agent2AgentProtocol.Client
{
    public class TransportManager
    {
        private readonly ConcurrentDictionary<string, IMessagingTransport> _transports = new();
        private readonly SemaphoreSlim _lock = new(1, 1);

        public async Task<IMessagingTransport> GetOrCreateTransportAsync(string pipeName, ILogger<NamedPipeTransport> logger)
        {
            if(_transports.TryGetValue(pipeName, out IMessagingTransport existingTransport))
            {
                return existingTransport;
            }

            await _lock.WaitAsync();
            try
            {
                // Double-check after acquiring lock
                if(_transports.TryGetValue(pipeName, out existingTransport))
                {
                    return existingTransport;
                }

                var transport = new NamedPipeTransport(pipeName, isServer: false, logger);
                _transports[pipeName] = transport;
                return transport;
            }
            finally
            {
                _lock.Release();
            }
        }
    }
}
