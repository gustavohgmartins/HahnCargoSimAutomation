using App.Domain.DTOs;

namespace App.Domain.Services
{
    public interface IOrderService
    {
        Task<bool> Generate(string token);
    }
}
