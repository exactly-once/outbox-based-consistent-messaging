using System;
using System.Collections.Generic;
using System.Threading.Tasks;

public class InMemoryInbox : IInboxStore
{
    Dictionary<string, Guid> data = new Dictionary<string, Guid>();

    public Task<DeduplicateResult> Deduplicate(string messageId, Guid claimId)
    {
        return Task.FromResult(DeduplicateInternal(messageId, claimId));
    }

    DeduplicateResult DeduplicateInternal(string messageId, Guid claimId)
    {
        lock (data)
        {
            if (data.TryGetValue(messageId, out var existingClaim))
            {
                return existingClaim == claimId
                    ? DeduplicateResult.RecordExists
                    : DeduplicateResult.Duplicate;
            }
            else
            {
                data[messageId] = claimId;
                return DeduplicateResult.RecordCreated;
            }
        }
    }
}