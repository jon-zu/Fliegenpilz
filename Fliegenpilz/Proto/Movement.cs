using Fliegenpilz.Net;

namespace Fliegenpilz.Proto;

public record MovementFooter(
    byte Action,
    DurationMs16 Duration
) : IEncodePacket, IDecodePacket<MovementFooter>
{
    public void EncodePacket(ref PacketWriter w)
    {
        w.WriteByte(Action);
        Duration.EncodePacket(ref w);
    }

    public static MovementFooter DecodePacket(ref PacketReader r)
        => new(
            r.ReadByte(),
            DurationMs16.DecodePacket(ref r)
        );
}

public enum MovementCode : byte
{
    Normal = 0,
    Jump = 1,
    Impact = 2,
    Immediate = 0x3,
    Teleport = 0x4,
    HangOnBack = 5,
    Assaulter = 0x6,
    Assassinate = 0x7,
    Rush = 0x8,
    StatChange = 0x9,
    SitDown = 0xA,
    StartFallDown = 0xB,
    FallDown = 0xC,
    StartWings = 0xD,
    Wings = 0xE,
    MobToss = 0x10,
    FlyingBlock = 0x11,
    DashSlide = 0x12,
    FlashJump = 0x14,
    RocketBooster = 0x15,
    BackstepShot = 0x16,
    MobPowerKnockback = 0x17,
    VerticalJump = 0x18,
    CustomImpact = 0x19,
    CombatStep = 0x1A,
    Hit = 0x1B,
    TimeBombAttack = 0x1C,
    SnowballTouch = 0x1D,
    BuffZoneEffect = 0x1E,
    MobLadder = 0x1F,
    MobRightAngle = 0x20,
    MobStopNodeStart = 0x21,
    MobBeforeNode = 0x22,
    MobAttackRush = 0x23,
    MobAttackRushStop = 0x24,
}

public abstract record Movement(MovementCode Code)
    : IEncodePacket, IDecodePacket<Movement>
{
    public static Movement DecodePacket(ref PacketReader reader)
    {
        var code = (MovementCode)reader.ReadByte();
        switch (code)
        {
            case MovementCode.Normal:
            case MovementCode.HangOnBack:
            case MovementCode.MobAttackRush:
            case MovementCode.MobAttackRushStop:
            case MovementCode.Wings:
                return new AbsoluteMovement(
                    code,
                    Vec2.DecodePacket(ref reader),
                    Vec2.DecodePacket(ref reader),
                    FootholdId.DecodePacket(ref reader),
                    FootholdId.DecodePacket(ref reader),
                    MovementFooter.DecodePacket(ref reader)
                );
            case MovementCode.Jump:
            case MovementCode.Impact:
            case MovementCode.StartWings:
            case MovementCode.MobLadder:
            case MovementCode.MobRightAngle:
            case MovementCode.MobStopNodeStart:
            case MovementCode.MobBeforeNode:
            case MovementCode.DashSlide:
            case MovementCode.MobToss:
                return new RelativeMovement(
                    code,
                    Vec2.DecodePacket(ref reader),
                    MovementFooter.DecodePacket(ref reader)
                );
            case MovementCode.Immediate:
            case MovementCode.Teleport:
            case MovementCode.SitDown:
            case MovementCode.Assaulter:
            case MovementCode.Assassinate:
            case MovementCode.Rush:
                return new InstantMovement(
                    code,
                    Vec2.DecodePacket(ref reader),
                    FootholdId.DecodePacket(ref reader),
                    MovementFooter.DecodePacket(ref reader)
                );
            case MovementCode.StatChange:
                return new StatChangeMovement(
                    code,
                    reader.ReadBool()
                );
            case MovementCode.StartFallDown:
                return new FallDownMovement(
                    code,
                    Vec2.DecodePacket(ref reader),
                    FootholdId.DecodePacket(ref reader),
                    MovementFooter.DecodePacket(ref reader)
                );
            case MovementCode.FallDown:
                return new AbsoluteFallMovement(
                    code,
                    Vec2.DecodePacket(ref reader),
                    Vec2.DecodePacket(ref reader),
                    FootholdId.DecodePacket(ref reader),
                    FootholdId.DecodePacket(ref reader),
                    Vec2.DecodePacket(ref reader),
                    MovementFooter.DecodePacket(ref reader)
                );
            case MovementCode.FlyingBlock:
                return new FlyingMovement(
                    code,
                    Vec2.DecodePacket(ref reader),
                    Vec2.DecodePacket(ref reader),
                    MovementFooter.DecodePacket(ref reader)
                );
            case MovementCode.FlashJump:
            case MovementCode.RocketBooster:
            case MovementCode.BackstepShot:
            case MovementCode.MobPowerKnockback:
            case MovementCode.VerticalJump:
            case MovementCode.CustomImpact:
            case MovementCode.CombatStep:
            case MovementCode.Hit:
            case MovementCode.TimeBombAttack:
            case MovementCode.SnowballTouch:
            case MovementCode.BuffZoneEffect:
                return new UnknownMovement(
                    code,
                    MovementFooter.DecodePacket(ref reader)
                );
            default:
                throw new NotImplementedException();
        }
    }

    public void EncodePacket(ref PacketWriter w)
    {
        w.WriteByte((byte)Code);
    }
}

public record AbsoluteMovement(
    MovementCode Code,
    Vec2 Pos,
    Vec2 Velocity,
    FootholdId Foothold,
    FootholdId FootholdFallStart,
    MovementFooter Footer
) : Movement(Code), IEncodePacket
{
    public new void EncodePacket(ref PacketWriter w)
    {
        base.EncodePacket(ref w);
        Pos.EncodePacket(ref w);
        Velocity.EncodePacket(ref w);
        Foothold.EncodePacket(ref w);
        FootholdFallStart.EncodePacket(ref w);
        Footer.EncodePacket(ref w);
    }
}

public record AbsoluteFallMovement(
    MovementCode Code,
    Vec2 Pos,
    Vec2 Velocity,
    FootholdId Foothold,
    FootholdId FootholdFallStart,
    Vec2 Offset,
    MovementFooter Footer
) : Movement(Code), IEncodePacket
{
    public new void EncodePacket(ref PacketWriter w)
    {
        base.EncodePacket(ref w);
        Pos.EncodePacket(ref w);
        Velocity.EncodePacket(ref w);
        Foothold.EncodePacket(ref w);
        FootholdFallStart.EncodePacket(ref w);
        Offset.EncodePacket(ref w);
        Footer.EncodePacket(ref w);
    }
}

public record RelativeMovement(
    MovementCode Code,
    Vec2 Velocity,
    MovementFooter Footer
) : Movement(Code), IEncodePacket
{
    public new void EncodePacket(ref PacketWriter w)
    {
        base.EncodePacket(ref w);
        Velocity.EncodePacket(ref w);
        Footer.EncodePacket(ref w);
    }
}

public record InstantMovement(
    MovementCode Code,
    Vec2 Pos,
    FootholdId Foothold,
    MovementFooter Footer
) : Movement(Code), IEncodePacket
{
    public new void EncodePacket(ref PacketWriter w)
    {
        base.EncodePacket(ref w);
        Pos.EncodePacket(ref w);
        Foothold.EncodePacket(ref w);
        Footer.EncodePacket(ref w);
    }
}

public record FallDownMovement(
    MovementCode Code,
    Vec2 Velocity,
    FootholdId FhFallStart,
    MovementFooter Footer
) : Movement(Code), IEncodePacket
{
    public new void EncodePacket(ref PacketWriter w)
    {
        base.EncodePacket(ref w);
        Velocity.EncodePacket(ref w);
        FhFallStart.EncodePacket(ref w);
        Footer.EncodePacket(ref w);
    }
}

public record FlyingMovement(
    MovementCode Code,
    Vec2 Pos,
    Vec2 Velocity,
    MovementFooter Footer
) : Movement(Code), IEncodePacket
{
    public new void EncodePacket(ref PacketWriter w)
    {
        base.EncodePacket(ref w);
        Pos.EncodePacket(ref w);
        Velocity.EncodePacket(ref w);
        Footer.EncodePacket(ref w);
    }
}

public record UnknownMovement(
    MovementCode Code,
    MovementFooter Footer
) : Movement(Code), IEncodePacket
{
    public new void EncodePacket(ref PacketWriter w)
    {
        base.EncodePacket(ref w);
        Footer.EncodePacket(ref w);
    }
}

public record StatChangeMovement(
    MovementCode Code,
    bool MovementAffecting
) : Movement(Code), IEncodePacket
{
    public new void EncodePacket(ref PacketWriter w)
    {
        base.EncodePacket(ref w);
        w.WriteBool(MovementAffecting);
    }
}