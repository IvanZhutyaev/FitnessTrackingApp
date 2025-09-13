using System;
using FitnessTrackerBack.Services.Interfaces;
using FitnessTrackerBack.Data;
using FitnessTrackerBack.DTO;

namespace FitnessTrackerBack.Services
{

    public class UserService : IUserService
    {
        private readonly AppDbContext _db;

        public UserService(AppDbContext db)
        {

            _db = db;

        }

        public async Task<UserProfileDto?> GetProfileAsync(string username)
        {

            var user = await _db.Users.FirstOrDefaultAsync(u => u.Username == username);
            if (user == null)
                return null;

            var profileData = new UserProfileDto
            {
                Username = user.Username,
                BirthDate = user.BirthDate,
                Weight = user.Weight,
                Height = user.Height,
                Goal = user.Goal,
                TargetWeight = user.TargetWeight,
                TargetPeriod = user.TargetPeriod
            };
            return profileData;



        }

        public async Task<bool?> DeleteUserAsync(int userid) => false; //soon

        public async Task<bool?> RegisterUserAsync(RegisterRequest request)
        {

            if (string.IsNullOrWhiteSpace(request.Username) || string.IsNullOrWhiteSpace(request.Password) || string.IsNullOrWhiteSpace(request.BirthDate))
                return false;

            var exists = await _db.Users.AnyAsync(user => user.Username == request.Username);
            if (exists)
                return false;

            var user = new User { Username = request.Username, Password = request.Password, BirthDate = request.BirthDate };
            _db.Users.Add(user);
            await _db.SaveChangesAsync();
            return true;


        }



    }

}
