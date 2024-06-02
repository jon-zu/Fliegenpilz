using System.Threading.Channels;

namespace Fliegenpilz.Scripts;

public abstract record NpcAction;

public record NpcActionStart : NpcAction;

public record NpcActionNextPrev(bool Next) : NpcAction;

public record NpcActionSelect(int Index) : NpcAction;

public record NpcActionText(string Text) : NpcAction;

public record NpcActionYesNo(bool Yes) : NpcAction;

public record NpcActionEnd : NpcAction;

public interface INpcScriptContext
{
    public Task SayNext(string text);
    public Task SayPrev(string text);
    public Task<NpcActionNextPrev> SayNextPrev(string text);
    public Task<NpcActionYesNo> SayYesNo(string text);
    public Task<NpcActionEnd> SayEnd(string text);
    public Task<NpcActionSelect> SaySelect(string text, string[] options);
    Task WaitForStart();

    public void Cancel();
    void SetException(Exception exception);
    void Complete();
}

public record Character(int Money, int Hp, int Level, int Job);

class Shared<T> : IDisposable
{
    private readonly ChannelWriter<NpcAction> _actionTx;
    private TaskCompletionSource<bool> _tcs;
    private Queue<string> _messages = new();
    private CancellationTokenSource _cts = new();
    private T _value;

    public Shared(T value, ChannelWriter<NpcAction> actionTx)
    {
        _value = value;
        _actionTx = actionTx;
        _tcs = new TaskCompletionSource<bool>();
    }

    public CancellationToken Token => _cts.Token;

    public void AddMessage(string message)
    {
        _messages.Enqueue(message);
    }

    public bool TryTakeMessage(out string msg) => _messages.TryDequeue(out msg!);

    public void AddAction(NpcAction action)
    {
        //TODO, if capacity is full the previous message was not handled
        if (!_actionTx.TryWrite(action))
            throw new InvalidOperationException();
    }

    public Task NextSuspendTask()
    {
        _tcs = new TaskCompletionSource<bool>();
        return _tcs.Task;
    }

    public void SignalSuspend()
    {
        _tcs.SetResult(true);
    }

    public void SetException(Exception e)
    {
        _tcs.SetException(e);
    }

    public void Dispose()
    {
        _cts.Dispose();
    }

    public void Cancel()
    {
        _cts.Cancel();
    }
}

public class NpcScriptContext<T> : INpcScriptContext
{
    private readonly ChannelReader<NpcAction> _actionRx;
    private readonly Shared<T> _shared;

    internal NpcScriptContext(ChannelReader<NpcAction> actionRx, Shared<T> shared)
    {
        _actionRx = actionRx;
        _shared = shared;
    }

    private ValueTask<NpcAction> ReadAction(bool signal = true)
    {
        if(signal)
            _shared.SignalSuspend();
        return _actionRx.ReadAsync(_shared.Token);
    }


    public async Task SayNext(string text)
    {
        Console.WriteLine(text);
        switch (await ReadAction())
        {
            case NpcActionNextPrev { Next: true }:
                break;
            default:
                throw new InvalidOperationException();
        }
    }

    public async Task SayPrev(string text)
    {
        Console.WriteLine(text);
        switch (await ReadAction())
        {
            case NpcActionNextPrev { Next: false }:
                break;
            default:
                throw new InvalidOperationException();
        }
    }

    public async Task<NpcActionNextPrev> SayNextPrev(string text)
    {
        Console.WriteLine(text);
        return await ReadAction() switch
        {
            NpcActionNextPrev action => action,
            _ => throw new InvalidOperationException()
        };
    }

    public async Task<NpcActionYesNo> SayYesNo(string text)
    {
        Console.WriteLine(text);
        return await ReadAction() switch
        {
            NpcActionYesNo action => action,
            _ => throw new InvalidOperationException()
        };
    }

    public async Task<NpcActionEnd> SayEnd(string text)
    {
        Console.WriteLine(text);
        return await ReadAction() switch
        {
            NpcActionEnd action => action,
            _ => throw new InvalidOperationException()
        };
    }

    public async Task<NpcActionSelect> SaySelect(string text, string[] options)
    {
        Console.WriteLine(string.Join(", ", options));
        return await ReadAction() switch
        {
            NpcActionSelect { Index: >= 0 } action when action.Index < options.Length => action,
            NpcActionSelect action => throw new InvalidOperationException(), // Check ix
            _ => throw new InvalidOperationException()
        };
    }

    public async Task WaitForStart()
    {
        if (await ReadAction(false) is not NpcActionStart) throw new InvalidOperationException();
    }

    public void Cancel()
    {
        _shared.Cancel();
    }

    public void SetException(Exception exception)
    {
        _shared.SetException(exception);
    }

    public void Complete()
    {
        _shared.SignalSuspend();
    }
}

public class NpcScriptHandle<T> : IDisposable
{
    private readonly Shared<T> _shared;
    private readonly Task _task;

    private NpcScriptHandle(Shared<T> shared, Task task)
    {
        _shared = shared;
        _task = task;
    }

    public static NpcScriptHandle<T> Launch(T value, INpcScript script)
    {
        var channel = Channel.CreateBounded<NpcAction>(1);
        var shared = new Shared<T>(value, channel.Writer);
        var ctx = new NpcScriptContext<T>(channel.Reader, shared);
        var task = Task.Run(() => RunScript(script, ctx));
        return new NpcScriptHandle<T>(shared, task);
    }

    private static async Task RunScript(INpcScript script, INpcScriptContext ctx)
    {
        try
        {
            await ctx.WaitForStart();
            await script.Run(ctx);
            ctx.Complete();
        }
        catch (Exception e)
        {
            ctx.Cancel();
            ctx.SetException(e);
            throw;
        }
    }

    public async Task Resume(NpcAction action)
    {
        var suspend = _shared.NextSuspendTask();
        _shared.AddAction(action);
        await suspend;
    }

    public void Dispose()
    {
        _task.Dispose();
        _shared.Dispose();
    }
}

public interface INpcScript
{
    public Task Run(INpcScriptContext ctx);
}

public class NpcScript1000 : INpcScript
{
    static async Task SayMultiple(INpcScriptContext ctx, string[] messages)
    {
        var i = 0;
        var n = messages.Length;
        while (i < n)
        {
            var msg = messages[i];
            if (i == 0)
            {
                await ctx.SayNext(msg);
                i++;
            }
            else
            {
                var action = await ctx.SayNextPrev(msg);
                if (action.Next) i++;
                else i--;
            }
        }
    }

    public async Task Run(INpcScriptContext ctx)
    {
        await ctx.SayNext("Hello");
        await ctx.SayNext("How are you?");
        var yesNo = await ctx.SayYesNo("Good?");
        if (yesNo.Yes)
        {
            await ctx.SayNext("Good");
        }
        else
        {
            await ctx.SayNext("Bad");
        }

        await SayMultiple(ctx, Enumerable.Range(0, 10)
            .Select(i => i.ToString())
            .ToArray()
        );


        var ix = await ctx.SaySelect("Select a number", Enumerable.Range(0, 10).Select(i => i.ToString()).ToArray());
        await ctx.SayNext($"You selected {ix.Index}");


        await ctx.SayEnd("Goodbye");
    }
}