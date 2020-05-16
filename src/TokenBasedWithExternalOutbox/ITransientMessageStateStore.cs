public interface ITransientMessageStateStore
{
    void MarkProcessed(string messageId);
    bool HasBeenProcessed(string messageId);
}