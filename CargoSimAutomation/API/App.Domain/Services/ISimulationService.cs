namespace App.Domain.Services
{
    public interface ISimulationService
    {
        Task<bool> Start(string token);
        Task<bool> Stop(string token);
    }
}
