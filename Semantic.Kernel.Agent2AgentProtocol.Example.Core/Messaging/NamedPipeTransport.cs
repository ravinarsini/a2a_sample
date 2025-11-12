using System.IO.Pipes;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace Semantic.Kernel.Agent2AgentProtocol.Example.Core.Messaging;

/// <summary>
/// Implementation of <see cref="IMessagingTransport"/> based on <see cref="NamedPipeServerStream"/> /
/// <see cref="NamedPipeClientStream"/>. The transport deals with **raw A2A JSON‑RPC messages** – every logical
/// message is framed on its own line (\n‑delimited).
/// </summary>
public sealed class NamedPipeTransport(string pipeName, bool isServer, ILogger<NamedPipeTransport>? logger = null) : IMessagingTransport
{
    private readonly string _pipeName = pipeName;
    private readonly bool _isServer = isServer;
    private Stream? _stream;
    private readonly ILogger<NamedPipeTransport>? _logger = logger;
    private Func<string, Task>? _handler;
    private CancellationTokenSource? _cts;

    public async Task StartProcessingAsync(Func<string, Task> onMessageReceived, CancellationToken cancellationToken)
    {
        _handler = onMessageReceived;
        _cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

        if(_isServer)
        {
            var server = new NamedPipeServerStream(
                _pipeName,
                PipeDirection.InOut,
                1,
                PipeTransmissionMode.Byte,
                PipeOptions.Asynchronous);

            await server.WaitForConnectionAsync(_cts.Token);
            _stream = server;
        }
        else
        {
            var client = new NamedPipeClientStream(
                ".",
                _pipeName,
                PipeDirection.InOut,
                PipeOptions.Asynchronous);

            await client.ConnectAsync(_cts.Token);
            _stream = client;
        }

        _ = Task.Run(ReadLoopAsync, _cts.Token); // fire‑and‑forget
    }

    private async Task ReadLoopAsync()
    {
        if(_stream == null || _handler == null)
            return;

        var reader = new StreamReader(_stream, Encoding.UTF8, detectEncodingFromByteOrderMarks: false, leaveOpen: true);
        try
        {
            while(!(_cts?.IsCancellationRequested ?? true))
            {
                string? line = await reader.ReadLineAsync(_cts?.Token ?? CancellationToken.None);

                // If line is null, the stream has been closed
                if(line == null)
                {
                    _logger?.LogDebug("Stream closed, exiting read loop");
                    break;
                }

                if(string.IsNullOrWhiteSpace(line))
                    continue;

                try
                {
                    // Validate that line is valid JSON so we don't forward garbage.
                    JsonDocument.Parse(line);
                    _logger?.LogDebug("Received JSON: {json}", line);
                    await _handler(line);
                }
                catch(JsonException ex)
                {
                    // Skip malformed payloads to avoid breaking the loop.
                    _logger?.LogWarning(ex, "Received malformed JSON");
                }
            }
        }
        catch(OperationCanceledException)
        {
            _logger?.LogDebug("Read loop cancelled");
        }
        catch(Exception ex)
        {
            _logger?.LogError(ex, "Error in read loop");
        }
    }

    public async Task SendMessageAsync(string json)
    {
        if(_stream is not { CanWrite: true })
            throw new InvalidOperationException("Pipe not connected.");

        // Use UTF8 without BOM to avoid 0xEF byte at start of stream
        await using var writer = new StreamWriter(_stream, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false), leaveOpen: true) { AutoFlush = true };
        await writer.WriteLineAsync(json);
        _logger?.LogDebug("Sent JSON: {json}", json);
    }

    public async Task StopProcessingAsync()
    {
        if(_cts != null)
            await _cts.CancelAsync();
        if(_stream != null)
            await _stream.DisposeAsync();
        _cts?.Dispose();
        await Task.Yield();
    }
}