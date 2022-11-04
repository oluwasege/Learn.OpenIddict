﻿using Microsoft.AspNetCore.Identity;
using System.Linq;
using System.Threading.Tasks;

namespace OPID
{
    public class SeedData
    {
        public static async Task SeedEssentialsAsync(UserManager<User> userManager, RoleManager<IdentityRole> roleManager)
        {
            //Seed Roles
            await roleManager.CreateAsync(new IdentityRole("Admin"));
            await roleManager.CreateAsync(new IdentityRole("User"));

            //Seed Default User
            var defaultUser = new User
            {
                UserName = "name",
                Email = "email.com",
                EmailConfirmed = true,
                PhoneNumberConfirmed = true,
                FirstName = "fname",
                LastName = "lname"
            };

            if (userManager.Users.All(u => u.Id != defaultUser.Id))
            {
                await userManager.CreateAsync(defaultUser, "password");
                await userManager.AddToRoleAsync(defaultUser, "admin");
            }

        }
    }
}
