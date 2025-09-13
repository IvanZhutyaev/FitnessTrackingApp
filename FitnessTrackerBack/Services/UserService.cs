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

        //   public async Task<bool?> DeleteUserAsync(int userid) => false; //soon

        public async Task<(bool Success, string Message)> RegisterUserAsync(RegisterRequest request)
        {

            if (string.IsNullOrWhiteSpace(request.Username) || string.IsNullOrWhiteSpace(request.Password) || string.IsNullOrWhiteSpace(request.BirthDate))
                return (false, "Enter all lines");

            var exists = await _db.Users.AnyAsync(user => user.Username == request.Username);
            if (exists)
                return (false, "User already exists");

            var user = new User { Username = request.Username, Password = request.Password, BirthDate = request.BirthDate };
            _db.Users.Add(user);
            await _db.SaveChangesAsync();
            return (true, "User registered!");


        }

        public async Task<(bool Success, string Message)> LoginAsync(LoginRequest request)
        {

            if (string.IsNullOrWhiteSpace(request.Username) || string.IsNullOrWhiteSpace(request.Password))
                return (false, "Enter all lines");


            var exists = await _db.Users.AnyAsync(user => user.Username == request.Username && user.Password == request.Password);
            if (!exists)
                return (false, "Incorrect username or password");

            return (true, "User logged in!");

        }
        public async Task<(bool, string)> UpdateProfileAsync(UserProfileDto request)
        {

            var user = await _db.Users.FirstOrDefaultAsync(u => u.Username == request.Username);
            if (user == null)
                return (true, "User not found");

            user.Goal = string.IsNullOrEmpty(request.Goal) ? "Похудение" : request.Goal;
            user.BirthDate = request.BirthDate;
            user.Weight = request.Weight;
            user.Height = request.Height;
            user.TargetWeight = request.TargetWeight;
            user.TargetPeriod = request.TargetPeriod;

            await _db.SaveChangesAsync();
            return (true, "Profile updated!");

        }


    }

}
