using System;
using System.Threading.Tasks;

public interface IInboxStore
{
    Task<DeduplicateResult> Deduplicate(string messageId, Guid claimId);
}