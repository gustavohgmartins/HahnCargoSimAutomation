using App.Domain.DTOs;

namespace App.Domain.Services
{
    public interface ISimulationService
    {
        Task<bool> Start(string token, string username);
        Task<bool> Stop(string token);
        IAutomation ManageUserAutomation(AuthDto authUser);
    }
}
