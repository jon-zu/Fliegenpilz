using Fliegenpilz.Actors;

public record NewSessionMessage(int roomId, GameSession session);

public record RoomHandle(int roomId, IActorRef actor);


public class WorldActor : UntypedActor
{
    private readonly Dictionary<int, RoomHandle> _rooms = [];
    private readonly Dictionary<SessionKey, GameSession> _sessions = [];

    private RoomHandle CreateOrAddRoom(int roomId)
    {
        if (_rooms.TryGetValue(roomId, out var room))
        {
            return room;
        }

        Console.WriteLine($"Creating room: {roomId}");
        var actor = Context.ActorOf(Props.Create(() => new RoomActor<GameRoom, GameSession>(new GameRoom())));
        var handle = new RoomHandle(roomId, actor);
        _rooms.Add(roomId, handle);
        return handle;
    }

    void AddNewSession(int roomId, GameSession session)
    {
        var room = CreateOrAddRoom(roomId);
        _sessions.Add(session.Key, session);
        room.actor.Tell(new JoinRoomMessage<GameSession>(session));
    }


    protected override void OnReceive(object message)
    {
        switch (message)
        {
            case NewSessionMessage msg:
                AddNewSession(msg.roomId, msg.session);
                break;
            default:
                throw new Exception("Unknown message type");
        }
    }
}