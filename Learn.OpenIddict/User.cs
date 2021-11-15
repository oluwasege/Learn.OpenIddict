using Microsoft.AspNetCore.Identity;

namespace Learn.OpenIddict
{
    public class User: IdentityUser<long>
    {
        public string FirstName { get; set; }
        public string LastName { get; set; }
    }
}
