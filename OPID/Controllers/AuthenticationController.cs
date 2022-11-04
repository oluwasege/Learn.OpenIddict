using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using OpenIddict.Abstractions;
using OpenIddict.Server.AspNetCore;
using System.Security.Claims;
using System.Threading.Tasks;
using static OpenIddict.Abstractions.OpenIddictConstants;

namespace OPID.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthenticationController : ControllerBase
    {
        //private readonly IOpenIddictApplicationManager _applicationManager;

        private readonly UserManager<User> _userManager;
        private readonly SignInManager<User> _signInManager;

        public AuthenticationController(UserManager<User> userManager, SignInManager<User> signInManager)
        {
            _userManager = userManager;
            _signInManager = signInManager;
        }

        [HttpPost("~/connect/token"),
            Consumes("application/x-www-form-urlencoded"),
            Produces("application/json")]
        public async Task<IActionResult> Exchange()
        {
            var request = HttpContext.GetOpenIddictServerRequest();

            if (request.IsPasswordGrantType())
                return await TokensForPasswordGrantType(request);

            //if (request.IsRefreshTokenGrantType())
            //{
            //    // return tokens for refresh token flow
            //}

            //if (request.GrantType == "custom_flow_name")
            //{
            //    // return tokens for custom flow
            //}

            return BadRequest(new OpenIddictResponse
            {
                Error = Errors.UnsupportedGrantType
            });
            #region ClientCredentialsGrantType
            //if (!request.IsClientCredentialsGrantType())
            //{
            //    throw new NotImplementedException("The specified grant is not implemented.");
            //}

            //// Note: the client credentials are automatically validated by OpenIddict:
            //// if client_id or client_secret are invalid, this action won't be invoked.

            //var application =
            //    await _applicationManager.FindByClientIdAsync(request.ClientId) ??
            //    throw new InvalidOperationException("The application cannot be found.");

            //// Create a new ClaimsIdentity containing the claims that
            //// will be used to create an id_token, a token or a code.
            //var identity = new ClaimsIdentity(
            //    TokenValidationParameters.DefaultAuthenticationType,
            //    Claims.Name, Claims.Role);

            //// Use the client_id as the subject identifier.
            //identity.AddClaim(Claims.Subject,
            //    await _applicationManager.GetClientIdAsync(application),
            //    Destinations.AccessToken, Destinations.IdentityToken);

            //identity.AddClaim(Claims.Name,
            //    await _applicationManager.GetDisplayNameAsync(application),
            //    Destinations.AccessToken, Destinations.IdentityToken);

            //return SignIn(new ClaimsPrincipal(identity),
            //    OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
            #endregion

        }

        private async Task<IActionResult> TokensForPasswordGrantType(OpenIddictRequest request)
        {
            var user = await _userManager.FindByNameAsync(request.Username);
            if (user == null)
                return Unauthorized("Username or password incorrect");

            var signInResult = await _signInManager.CheckPasswordSignInAsync(user, request.Password, false);
            if (signInResult.Succeeded)
            {
                var identity = new ClaimsIdentity(
                    TokenValidationParameters.DefaultAuthenticationType,
                    Claims.Name,
                    Claims.Role);
                var rolesList = await _userManager.GetRolesAsync(user).ConfigureAwait(false);
                identity.AddClaim(Claims.Subject, user.Id.ToString(), Destinations.AccessToken);
                identity.AddClaim(Claims.Username, user.UserName, Destinations.AccessToken);
                identity.AddClaim(Claims.Name, user.FirstName, Destinations.AccessToken);
                identity.AddClaim(Claims.Name, user.FirstName, Destinations.AccessToken);
                // Add more claims if necessary

                foreach (var userRole in rolesList)
                {
                    identity.AddClaim(Claims.Role, userRole.ToString(), OpenIddictConstants.Destinations.AccessToken);
                }

                var claimsPrincipal = new ClaimsPrincipal(identity);
                claimsPrincipal.SetScopes(new string[]
                {
                    Scopes.Roles,
                    Scopes.OfflineAccess,
                    Scopes.Email,
                    Scopes.Profile,
                });

                return SignIn(claimsPrincipal, OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
            }
            else
                return Unauthorized("Username or password incorrect");
        }
    }
}
