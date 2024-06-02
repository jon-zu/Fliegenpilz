using Fliegenpilz.Net;
using Fliegenpilz.Tick;

namespace Fliegenpilz.Actors;

public interface IRoom<TSelf, TSession> 
    where TSelf: IRoom<TSelf, TSession>
    where TSession: IGameSession<TSession, TSelf>
{
    void HandleTick(GameTime t);
}


public class Context<TRoom, TSession>
    where TRoom: IRoom<TRoom, TSession>
    where TSession: IGameSession<TSession, TRoom>
{
    public IList<TSession> Sessions { get; } = new List<TSession>();
    public TRoom Room { get; }

    public Context(TRoom room) 
    {
        Room = room;
    }

    public void Broadcast(SocketMessage packet)
    {
        foreach(var session in Sessions)
        {
            session.SendPacket(packet);
        }
    }

    public void BroadcastFilter(SocketMessage packet, SessionKey filterKey)
    {
        foreach (var session in Sessions)
        {
            if (session.Key != filterKey)
            {
                session.SendPacket(packet);
            }

        }
    }
    
    public void SendTo(SessionKey key, SocketMessage packet)
    {
        var session = Sessions.FirstOrDefault(s => s.Key == key);
        if(session != null)
        {
            session.SendPacket(packet);
        }
    }
    
}

public record JoinRoomMessage<TSession>(TSession session);


public class RoomActor<TRoom, TSession>(TRoom room) : UntypedActor, IWithTimers
    where TRoom: IRoom<TRoom, TSession>
    where TSession: IGameSession<TSession, TRoom>
{
    protected Context<TRoom, TSession> _ctx = new(room);

    private GameTime _time = new(0);

    public ITimerScheduler Timers { get; set; }

    protected override void PreStart()
    {
        Timers.StartPeriodicTimer(
            "Tick",
            new TickMessage(),
            TimeSpan.FromMilliseconds(50)
        );

    }

    private bool TickSession(TSession session, GameTime t)
    {
        try
        {
            session.Tick(t, _ctx);
            return true;
        }
        catch (Exception e)
        {
            //TODO log and avoid eating all exceptions
            Console.WriteLine(e);
            return false;
        }
    }

    private void TickSessions(GameTime t)
    {
        for(var i = 0; i < _ctx.Sessions.Count;)
        {
            if(TickSession(_ctx.Sessions[i], t))
            {
                i++;
                continue;
            }
            
            // TODO handle leave
            Console.WriteLine($"Session left {i}");
            _ctx.Sessions.RemoveAt(i);
        }
    }

    private void HandleTick(GameTime t)
    {
        _ctx.Room.HandleTick(t);
        TickSessions(t);
    }

    protected override void OnReceive(object message)
    {
        switch (message)
        {
            case JoinRoomMessage<TSession> join:
                _ctx.Sessions.Add(join.session);
                join.session.OnEnterRoom(_ctx);
                break;
            case TickMessage _:
                HandleTick(_time);
                _time = _time.Add(1);
                break;
            default:
                throw new Exception("Unknown message type");
        }
    }
}