namespace AI_LMS.Application.Interfaces;

public interface IPasswordService
{
    string Hash(string password);
    bool Verify(string password, string passwordHash);
}
