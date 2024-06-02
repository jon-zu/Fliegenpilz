using Akka.Util.Internal;

namespace Fliegenpilz.Tick;

public class Clock
{
    private AtomicCounterLong _ticks;
    private PeriodicTimer _timer;

    public Clock()
    {
        _ticks = new AtomicCounterLong(1);
        _timer = new PeriodicTimer(TimeSpan.FromMilliseconds(50));
    }

    public async Task Run()
    {
        while (true)
        {
            _ticks.Next();
            await _timer.WaitForNextTickAsync();
        }
    }
}

public struct GameTime
{
    public long Ticks { get; }

    public GameTime(long ticks)
    {
        Ticks = ticks;
    }

    public GameTime Add(long ticks) => new GameTime(Ticks + ticks);
}