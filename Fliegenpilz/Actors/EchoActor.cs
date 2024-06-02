using Akka.Streams.Dsl;
using Fliegenpilz.Net;

namespace Fliegenpilz.Actors;

public class TickMessage;

public class EchoActor(SocketHandle sckHandle) : UntypedActor, IWithTimers
{
    protected override void PreStart()
    {
        Timers.StartPeriodicTimer(
            "Tick",
            new TickMessage(),
            TimeSpan.FromMilliseconds(50)
        );
        ChannelSource.FromReader(sckHandle.Rx)
            .To(Sink.ActorRef<SocketMessage>(this.Self, new SocketClosedMessage(), (e) => new SocketClosedMessage()))
            .Run(Context.System);

        Console.WriteLine("EchoActor started");
    }


    protected override void OnReceive(object message)
    {
        switch (message)
        {
            case SocketMessage msg:
                sckHandle.TrySend(msg);
                break;
            case SocketClosedMessage _:
                Context.Stop(Self);
                Console.WriteLine("Socket closed");
                break;
            case TickMessage _:
                break;
            default:
                throw new Exception("Unknown message type");
        }
    }

    public ITimerScheduler Timers { get; set; }
}