using System;
using System.Collections.Generic;
using System.Threading.Tasks;

public class InMemoryInbox : IInboxStore
{
    Dictionary<string, Guid> data = new Dictionary<string, Guid>();

    public async Task<bool> TryClaim(string messageId, Guid claimId)
    {
        lock (data)
        {
            if (data.ContainsKey(messageId))
            {
                return false;
            }
            else
            {
                data[messageId] = claimId;
                return true;
            }
        }
    }

    public async Task<bool> IsClaimedBy(string messageId, Guid claimId)
    {
        lock (data)
        {
            return data.TryGetValue(messageId, out var existingClaim) && existingClaim == claimId;
        }
    }
}