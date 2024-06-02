using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fliegenpilz.Scripts;
using JetBrains.Annotations;
using Xunit;

namespace Fliegenpilz.Tests.Scripts;

[TestSubject(typeof(NpcScriptHandle<Character>))]
public class NpcScriptHandleTest
{

    [Fact]
    public async Task Basic()
    {
        var character = new Character(0, 100, 1, 1);
        
        var script = NpcScriptHandle<Character>.Launch(character, new NpcScript1000());
        Thread.Sleep(100);
        await script.Resume(new NpcActionStart());
        await script.Resume(new NpcActionNextPrev(true));
        await script.Resume(new NpcActionNextPrev(true));
        await script.Resume(new NpcActionYesNo(true));
        await script.Resume(new NpcActionNextPrev(true));

        foreach (var i in Enumerable.Range(0, 10))
            await script.Resume(new NpcActionNextPrev(true));

        await script.Resume(new NpcActionSelect(5));
        await script.Resume(new NpcActionNextPrev(true));
        await script.Resume(new NpcActionEnd());
    }
}