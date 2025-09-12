
using System.Collections.Generic;

namespace FitnessTracker.Services.Interfaces
{
    public interface IUserService
    {

        Task<string?> RegisterUserAsync(string username, string password, string birthDate);


        Task<string?> LoginAsync(string username, string password);


        Task<UserProfileDto?> GetProfileAsync(int userId);


        Task<bool> UpdateProfileAsync(int userId, ChangeInfoRequest update);


        Task<bool> DeleteUserAsync(int userId);
    }
}
