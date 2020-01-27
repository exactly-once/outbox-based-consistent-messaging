using System;
using System.Threading.Tasks;

public interface IInboxStore
{
    Task<bool> TryClaim(string messageId, Guid claimId);
    Task<bool> IsClaimedBy(string messageId, Guid claimId);
}