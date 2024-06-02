using System;
using Fliegenpilz.Net;
using Fliegenpilz.Proto;
using Fliegenpilz.Tick;

namespace Fliegenpilz.Actors;


public readonly struct SessionKey : IEquatable<SessionKey>
{
    public SessionKey(int id) => Id = id;


    private int Id { get; init; }

    public override bool Equals(object? obj) => obj is SessionKey other && this.Equals(other);
    public bool Equals(SessionKey other) => Id == other.Id;
    public override int GetHashCode() => Id;

    public override string ToString() => Id.ToString();

    public static bool operator ==(SessionKey left, SessionKey right) => left.Equals(right);
    public static bool operator !=(SessionKey left, SessionKey right) => !left.Equals(right);
}


public interface IGameSession<TSelf, TRoom>
    where TSelf : IGameSession<TSelf, TRoom>
    where TRoom : IRoom<TRoom, TSelf>
{
    public SessionKey Key { get; }

    void SendPacket(SocketMessage packet);

    public void OnEnterRoom(Context<TRoom, TSelf> ctx);

    public void Tick(GameTime t, Context<TRoom, TSelf> ctx);
}


public class GameRoom : IRoom<GameRoom, GameSession>
{
    public void HandleTick(GameTime t)
    {
        //throw new NotImplementedException();
    }
}



public class GameSession : IGameSession<GameSession, GameRoom>
{
    private readonly SocketHandle _sckHandle;

    public SessionKey Key { get; set; }

    public GameSession(SocketHandle sckHandle, SessionKey key)
    {
        _sckHandle = sckHandle;
        Key = key;
    }

    public void SendPacket(SocketMessage packet)
    {
        _sckHandle.TrySend(packet);
    }


    void HandleChatMessage(ChatMsgReq msg)
    {
        var pw = new PacketWriter();
        pw.WriteOpcode(SendOpcodes.UserChat);
        new UserChatMsgResp
        {
            CharacterId = 7,
            IsAdmin = false,
            Msg = msg.Msg,
            OnlyBalloon = false
        }.EncodePacket(ref pw);

        // Print hex buffer
        var buf = pw.DetachBuffer();
        Console.WriteLine(BitConverter.ToString(buf.Memory.ToArray()).Replace("-", " "));

        _sckHandle.TrySend(new SocketMessage(buf));
    }

    void HandlePacket(SocketMessage packet)
    {
        var opcode = (RecvOpcodes)packet.Opcode;
        Console.WriteLine($"Received opcode: {opcode} - Length: {packet.Inner.Length}");
        switch(opcode)
        {
            case RecvOpcodes.UserChat:
                var chatMsg = packet.Decode<ChatMsgReq>();
                Console.WriteLine($"Received chat message: {chatMsg.Msg}");
                HandleChatMessage(chatMsg);
                break;
        }
    }

    public void Tick(GameTime t, Context<GameRoom, GameSession> ctx)
    {
        // Read all pending messages
        while (_sckHandle.Rx.TryRead(out var msg))
        {
            using var sckMsg = msg!;
            HandlePacket(sckMsg);
            //ctx.Broadcast(msg);
        }

        if (_sckHandle.Rx.Completion.IsCompleted)
        {
            Console.WriteLine("Socket closed");
            throw new Exception("Socket closed");
        }

        // Do internal game logic
    }

    public void OnEnterRoom(Context<GameRoom, GameSession> ctx)
    {
        Console.WriteLine("Entered room");
        var pw = new PacketWriter();
        pw.WriteOpcode(SendOpcodes.SetField);
        new SetFieldResp(
            1,
            0,
            0,
            FileTime.FromDateTime(DateTime.UtcNow)
        ).EncodePacket(ref pw);

        _sckHandle.TrySend(new SocketMessage(pw.DetachBuffer()));

        Console.WriteLine("Sent SetFieldResp");
    }
}


public record CrcSeed(
    int s1,
    int s2,
    int s3
) : IEncodePacket
{
    public void EncodePacket(ref PacketWriter w)
    {
        w.WriteInt(s1);
        w.WriteInt(s2);
        w.WriteInt(s3);
    }
}

public record CharName(string name) : IEncodePacket
{
    public void EncodePacket(ref PacketWriter w)
    {
        w.WriteFixedSizeString(name, 13);
    }

}

public enum SkinColor : byte
{
    Normal = 0,
    Dark = 1,

    Black = 2,
    Pale = 3,
    Blue = 4,
    Green = 5,
    White = 9,
    Pink = 10
}

public enum Gender : byte
{
    Male = 0,
    Female = 1
}

public record CharStats(
    int Id,
    string Name,
    Gender Gender,
    SkinColor SkinColor,
    int FaceId, // 20000
    int HairId, //30000,
    byte Level,
    short JobId,// 100,
    short Str,
    short Dex,
    short Int,
    short Luk,
    int Hp,
    int MaxHp,
    int Mp,
    int MaxMp,
    short Ap,
    short Sp,
    int Exp,
    short Pop,
    int TmpExp,
    int FieldId,
    byte Portal,
    int Playtime,
    short SubJob
) : IEncodePacket
{
    public void EncodePacket(ref PacketWriter w)
    {
        w.WriteInt(Id);
        new CharName(Name).EncodePacket(ref w);
        w.WriteByte((byte)Gender);
        w.WriteByte((byte)SkinColor);
        w.WriteInt(FaceId);
        w.WriteInt(HairId);

        // TODO pets
        for (int i = 0; i < 3; i++)
        {
            w.WriteLong(0);
        }

        w.WriteByte(Level);
        w.WriteShort(JobId);

        w.WriteShort(Str);
        w.WriteShort(Dex);
        w.WriteShort(Int);
        w.WriteShort(Luk);

        w.WriteInt(Hp);
        w.WriteInt(MaxHp);
        w.WriteInt(Mp);
        w.WriteInt(MaxMp);

        w.WriteShort(Ap);
        //TODO skill pages
        w.WriteShort(Sp);

        w.WriteInt(Exp);
        w.WriteShort(Pop);
        w.WriteInt(TmpExp);

        w.WriteInt(FieldId);
        w.WriteByte(Portal);

        w.WriteInt(Playtime);
        w.WriteShort(SubJob);

        //TODO friend max
        w.WriteByte(30);
        // Linked char
        w.WriteByte(0);
    }
}

[Flags]
enum CharDataFlags : int
{
    Stat,
    Money,
    Equipped
}

public record SetFieldResp(
    int ChannelId,
    int OldDriverId,
    byte FieldKey,

    FileTime ServerTime

) : IEncodePacket
{
    public void EncodePacket(ref PacketWriter w)
    {
        w.WriteShort(0);
        w.WriteInt(ChannelId);
        w.WriteInt(OldDriverId);
        w.WriteByte(FieldKey);
        // TODO: flag for either char data or transfer data
        w.WriteByte(1);
        // TODO: Empty Notification list
        w.WriteShort(0);

        // Char data
        //TODO
        new CrcSeed(0, 0, 0).EncodePacket(ref w);

        long flags = 1;


        w.WriteLong(flags);
        // TODO combat orders
        w.WriteByte(0);
        // TODO extra data
        w.WriteByte(0);

        // Char stats
        new CharStats(
            7,
            "Test",
            0,
            SkinColor.Normal,
            20000,
            30000,
            177,
            100,
            4,
            4,
            4,
            4,
            100,
            100,
            100,
            100,
            0,
            0,
            0,
            10,
            0,
            1000000,
            0,
            0,
            0
        ).EncodePacket(ref w);


        // Logout gift at the end
        w.WriteInt(0); // Predict quit
        w.WriteInt(0); // gift 1
        w.WriteInt(0); // gift 2
        w.WriteInt(0); // gift 3


        //w.WriteLong(0);
        w.WriteTime(ServerTime);
    }
}