
using System.Collections.Generic;

namespace FitnessTrackerBack.Services.Interfaces
{
    public interface IUserService
    {

        Task<(bool Success, string Message)> RegisterAsync(RegisterRequest request);


        Task<(bool Success, string Message)> LoginAsync(LoginRequest request);


        Task<UserProfileDto?> GetProfileAsync(string username);


        Task<(bool Success, string Message)> UpdateProfileAsync(UserProfileDto request);


        //       Task<bool> DeleteUserAsync(int userId);
    }
}
