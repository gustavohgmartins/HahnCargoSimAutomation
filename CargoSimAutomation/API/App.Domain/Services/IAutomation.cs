using App.Domain.DTOs;

namespace App.Domain.Services
{
    public interface IAutomation
    {
        Task Start(string token);
        Task Stop();
    }
}
