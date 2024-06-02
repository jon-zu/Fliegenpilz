using Fliegenpilz.Net;

namespace Fliegenpilz.Proto;

/* 
#[derive(Debug, ShroomPacket)]
pub struct ChatMsgReq {
    pub ticks: Ticks,
    pub msg: String,
    pub only_balloon: bool,
}
with_opcode!(ChatMsgReq, RecvOpcodes::UserChat);
*/


public class ChatMsgReq : IDecodePacket<ChatMsgReq>
{
    public int Ticks { get; set; }
    public required string Msg { get; set; }
    public bool OnlyBalloon { get; set; }

    public static ChatMsgReq DecodePacket(ref PacketReader reader)
    {
        return new ChatMsgReq
        {
            Ticks = reader.ReadInt(),
            Msg = reader.ReadString(),
            OnlyBalloon = reader.ReadBool()
        };
    }
}

/*

#[derive(ShroomPacket, Debug)]
pub struct UserChatMsgResp {
    pub char: CharacterId,
    pub is_admin: bool,
    pub msg: String,
    pub only_balloon: bool,
}
with_opcode!(UserChatMsgResp, SendOpcodes::UserChat);
*/

public class UserChatMsgResp : IEncodePacket
{
    public int CharacterId { get; set; }
    public bool IsAdmin { get; set; }
    public required string Msg { get; set; }
    public bool OnlyBalloon { get; set; }

    public void EncodePacket(ref PacketWriter w)
    {
        w.WriteInt(CharacterId);
        w.WriteBool(IsAdmin);
        w.WriteString(Msg);
        w.WriteBool(OnlyBalloon);
    }
}