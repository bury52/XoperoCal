using System.Buffers.Text;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;
using xopCal.Entity;
using xopCal.Exceptions;
using xopCal.Model;

namespace xopCal.Services;

public class AuthService : IAuthService
{
    
    // AuthenticationSettings authSetting = new AuthenticationSettings()
    // {
    //
    //     JwtKey= "PRIVATE_KEY_DONT_SHARE",
    //     JwtExpireDays= 15,
    //     JwtIssuer= "http://eventcalapi.com"
    //
    // };

    private readonly EventDbContext _context;
    private readonly IPasswordHasher<User> _passwordHasher;
    private readonly AuthenticationSettings _authSetting;

    public AuthService(EventDbContext context, IPasswordHasher<User> passwordHasher,AuthenticationSettings authSetting)
    {
        _context = context;
        _passwordHasher = passwordHasher;
        _authSetting = authSetting;
    }

    public void RegisterUser(UserDtoIn dtoIn)
    {
        var user = new User()
        {
            
            Name = dtoIn.Name,
            Email = dtoIn.Email,
            Color = dtoIn.Color

        };
        var hasedPasword = _passwordHasher.HashPassword(user, dtoIn.Password);
        user.PasswordHash = hasedPasword;
        _context.Users.Add(user);
        _context.SaveChanges();

    }

    public string GetJwt(LoginDto dto)
    {
        var user = _context.Users.FirstOrDefault(u => u.Email == dto.Email);

        if (user is null)
        {
            // throw new BadRequestException("Invalid email or password");
            return "error Invalid email or password";
        }

        var result = _passwordHasher.VerifyHashedPassword(user, user.PasswordHash, dto.Password);
        
        if (result == PasswordVerificationResult.Failed)
        {
            // throw new BadRequestException("Invalid email or password");
            return "error Invalid email or password";
        }

        var claims = new List<Claim>()
        {
            new Claim(ClaimTypes.Email,user.Email),
            new Claim(ClaimTypes.NameIdentifier,user.Id.ToString()),
            new Claim(ClaimTypes.Name,user.Name)
        };

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_authSetting.JwtKey));
        var cred = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var expires = DateTime.Now.AddDays(_authSetting.JwtExpireDays);

        var token = new JwtSecurityToken(_authSetting.JwtIssuer, _authSetting.JwtIssuer, claims, expires: expires , signingCredentials:cred);

        var tokenHandler = new JwtSecurityTokenHandler();
        return tokenHandler.WriteToken(token);

    }
}