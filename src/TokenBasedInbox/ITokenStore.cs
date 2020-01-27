using System.Threading.Tasks;

public interface ITokenStore
{
    Task<Token> Get(string messageId);
    Task Update(Token token);
    Task Delete(Token token);
    Task Create(Token token);
}