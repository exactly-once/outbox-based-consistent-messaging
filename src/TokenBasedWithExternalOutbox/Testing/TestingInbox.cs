using System;
using System.Threading.Tasks;

class TestingTokenStore : ITokenStore
{
    Func<string, Task> barrier;
    ITokenStore impl;

    public TestingTokenStore(Func<string, Task> barrier, ITokenStore impl)
    {
        this.barrier = barrier;
        this.impl = impl;
    }

    public async Task Delete(string messageId)
    {
        await impl.Delete(messageId).ConfigureAwait(false);
        await barrier("TokenStore.Delete").ConfigureAwait(false);
    }

    public async Task<bool> Exists(string id)
    {
        var result = await impl.Exists(id).ConfigureAwait(false);
        await barrier("TokenStore.Exists").ConfigureAwait(false);
        return result;
    }

    public void Create(string messageId)
    {
        impl.Create(messageId);
    }
}