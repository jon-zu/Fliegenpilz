using System.Buffers;
using System.Net;
using Akka.Hosting;
using Akka.Util.Internal;
using Fliegenpilz.Actors;
using Fliegenpilz.Net;
using Microsoft.AspNetCore.SignalR.Protocol;




var builder = WebApplication.CreateBuilder(args);
builder.Services.AddAkka("MyActorSystem", (builder, sp) =>
{

});


var app = builder.Build();
var system = app.Services.GetRequiredService<ActorSystem>();
var world = system.ActorOf(Props.Create(() => new WorldActor()));

var webSocketOptions = new WebSocketOptions
{
    KeepAliveInterval = TimeSpan.FromMinutes(2)
};
app.UseWebSockets(webSocketOptions);


AtomicCounterLong lastSessionId = new(1);

app.Use(async (context, next) =>
{
    if (context.Request.Path == "/ws")
    {
        if (context.WebSockets.IsWebSocketRequest)
        {
            Console.WriteLine("WebSocket request");


            var ip = context.Connection.RemoteIpAddress ?? IPAddress.Loopback;
            var webSocket = await context.WebSockets.AcceptWebSocketAsync();

            using var sck = new Socket(webSocket, ip);
            var sckTask = sck.Run();
            var handle = sck.GetHandle();


            // Read handshake
            Handshake handshake;
            using(var msg = await handle.Rx.ReadAsync()) {
                var opcode = msg.Opcode;
                if(opcode != 0x214) {
                    throw new Exception("Expected handshake opcode");
                }

                handshake = msg.Decode<Handshake>();
            }

            Console.WriteLine($"Handshake: {handshake}");

            var sessId = lastSessionId.GetAndIncrement();
            world.Tell(new NewSessionMessage(1, new GameSession(handle, new SessionKey((int)sessId))));
            await sckTask;
        }
        else
        {
            context.Response.StatusCode = StatusCodes.Status400BadRequest;
        }
    }
    else
    {
        await next(context);
    }
});



var completionTask = app.RunAsync();
await completionTask; // wait for the host to shut down


public record Handshake(
    int characterId,
    int accountId,
    long clientKey,
    byte[] token
) : IEncodePacket, IDecodePacket<Handshake>
{
    public static Handshake DecodePacket(ref PacketReader reader)
    {
        var characterId = reader.ReadInt();
        var accountId = reader.ReadInt();
        var clientKey = reader.ReadLong();
        var token = reader.ReadBytes(32).ToArray();
        return new Handshake(characterId, accountId, clientKey, token);
    }

    public void EncodePacket(ref PacketWriter w)
    {
        w.WriteInt(characterId);
        w.WriteInt(accountId);
        w.WriteLong(clientKey);
        w.WriteBytes(token);
    }
}