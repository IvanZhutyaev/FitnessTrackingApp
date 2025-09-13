
using System.Collections.Generic;

namespace FitnessTrackerBack.Services.Interfaces
{
    public interface IUserService
    {

        Task<(bool, string)> RegisterUserAsync(RegisterRequest request);


        Task<(bool, string)> LoginAsync(LoginRequest request);


        Task<UserProfileDto?> GetProfileAsync(string username);


        Task<(bool, string)> UpdateProfileAsync(UserProfileDto request);


        //       Task<bool> DeleteUserAsync(int userId);
    }
}
