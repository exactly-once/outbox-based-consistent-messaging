using System;
using System.Threading.Tasks;

public interface ITokenStore
{
    Task Delete(string messageId);
    Task<bool> Exists(string id);
    void Create(string messageId);
}