using System.Buffers;
using System.Net;
using System.Net.WebSockets;
using System.Threading.Channels;
using DotNext.Buffers;

namespace Fliegenpilz.Net;

public class SocketMessage(MemoryOwner<byte> data) : IDisposable
{
    public MemoryOwner<byte> Inner => data;


    public void Dispose()
    {
        data.Dispose();
    }

    public short Opcode => new PacketReader(new ReadOnlySequence<byte>(data.Memory)).ReadShort();

    public T Decode<T>() where T : IDecodePacket<T>
    {
        var reader = new PacketReader(new ReadOnlySequence<byte>(data.Memory));
        // Skip opcode
        reader.ReadShort();
        return T.DecodePacket(ref reader);
    }
}

public class SocketClosedMessage;

public class SocketHandle(ChannelWriter<SocketMessage> tx, ChannelReader<SocketMessage> rx, IPAddress ipAddress)
{
    public ChannelReader<SocketMessage> Rx => rx;
    public IPAddress IpAddress => ipAddress;

    public async Task Send(SocketMessage data)
    {
        await tx.WriteAsync(data);
    }

    public bool TrySend(SocketMessage data)
    {
        //TODO check result
        return tx.TryWrite(data);
    }

}

public class Socket : IDisposable
{
    private readonly WebSocket _sck;
    private readonly IPAddress _ipAddress;
    private readonly ChannelReader<SocketMessage> _sendRx;
    private readonly ChannelWriter<SocketMessage> _sendTx;

    private readonly ChannelReader<SocketMessage> _recvRx;
    private readonly ChannelWriter<SocketMessage> _recvTx;

    private readonly CancellationTokenSource _cts = new();

    public Socket(WebSocket sck, IPAddress ipAddress)
    {
        _sck = sck;
        _ipAddress = ipAddress;

        var send = Channel.CreateBounded<SocketMessage>(128);
        _sendRx = send.Reader;
        _sendTx = send.Writer;

        var recv = Channel.CreateBounded<SocketMessage>(128);
        _recvRx = recv.Reader;
        _recvTx = recv.Writer;
    }

    public SocketHandle GetHandle()
    {
        return new SocketHandle(_sendTx, _recvRx, _ipAddress);
    }

    private async Task ReaderTask()
    {
        const int CHUNK_SIZE = 1024;

        try
        {
            using var writer = new PoolingBufferWriter<byte>(ArrayPool<byte>.Shared.ToAllocator());
            while (!_cts.IsCancellationRequested)
            {
                var res = await _sck.ReceiveAsync(writer.GetMemory(CHUNK_SIZE), _cts.Token);
                writer.Advance(res.Count);

                while (!res.EndOfMessage)
                {
                    res = await _sck.ReceiveAsync(writer.GetMemory(CHUNK_SIZE), _cts.Token);
                    writer.Advance(res.Count);
                }

                if (res.MessageType != WebSocketMessageType.Binary && res.MessageType != WebSocketMessageType.Text)
                    throw new InvalidOperationException("Only binary messages are supported");

                await _recvTx.WriteAsync(new SocketMessage(writer.DetachBuffer()));
            }
        }
        catch (Exception e)
        {
            Console.WriteLine("Reader task finished with exception: " + e.Message);
        }
        finally
        {
            await _cts.CancelAsync();
            _recvTx.Complete();
        }
    }

    private async Task WriterTask()
    {
        try
        {
            await foreach (var data in _sendRx.ReadAllAsync(_cts.Token))
            {
                await _sck.SendAsync(data.Inner.Memory, WebSocketMessageType.Binary, true, _cts.Token);
            }
        }
        catch (Exception e)
        {
            Console.WriteLine("Writer task finished with exception: " + e.Message);
        }
        finally
        {
            await _cts.CancelAsync();
        }
    }

    public async Task Run()
    {
        await Task.WhenAll(
            Task.Run(() => ReaderTask()),
            Task.Run(WriterTask)
        );
    }

    public void Dispose()
    {
        _sck.Dispose();
        _cts.Dispose();
    }
}