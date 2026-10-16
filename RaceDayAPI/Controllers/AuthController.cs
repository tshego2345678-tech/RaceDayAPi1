using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using RaceDayApi.Data;
using RaceDayApi.Models;
namespace RaceDayApi.Controllers
{
    [Route("api/auth")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly RaceDayContext _context;
        private readonly IConfiguration _configuration;
        private readonly PasswordHasher<Participant> _participantHasher = new();
        private readonly PasswordHasher<Organiser> _organiserHasher = new();
        public AuthController(
            RaceDayContext context,
            IConfiguration configuration)
        {
            _context = context;
            _configuration = configuration;
        }
        public class RegisterParticipantRequest
        {
            public string FullName { get; set; } = string.Empty;
            public string Email { get; set; } = string.Empty;
            public string Password { get; set; } = string.Empty;
            public DateTime? DateOfBirth { get; set; }
        }
        public class RegisterOrganiserRequest
        {
            public string FullName { get; set; } = string.Empty;
            public string Email { get; set; } = string.Empty;
            public string Password { get; set; } = string.Empty;
            public string OrganisationName { get; set; } = string.Empty;
        }
        public class LoginRequest
        {
            public string Email { get; set; } = string.Empty;
            public string Password { get; set; } = string.Empty;
        }
        [HttpPost("register/participant")]
        public async Task<IActionResult> RegisterParticipant(
            RegisterParticipantRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.FullName) ||
                string.IsNullOrWhiteSpace(request.Email) ||
                string.IsNullOrWhiteSpace(request.Password))
            {
                return BadRequest("Full name, email and password are required.");
            }
            if (request.Password.Length < 8)
            {
                return BadRequest("Password must be at least 8 characters.");
            }
            var email = request.Email.Trim().ToLowerInvariant();
            if (await _context.Participants.AnyAsync(p => p.Email == email) ||
                await _context.Organisers.AnyAsync(o => o.Email == email))
            {
                return Conflict("Email is already registered.");
            }
            var participant = new Participant
            {
                FullName = request.FullName.Trim(),
                Email = email,
                DateOfBirth = request.DateOfBirth
            };
            participant.PasswordHash =
                _participantHasher.HashPassword(
                    participant, request.Password);
            _context.Participants.Add(participant);
            await _context.SaveChangesAsync();
            return Ok(new
            {
                message = "Participant registered successfully.",
                participant.ParticipantId,
                participant.FullName,
                participant.Email
            });
        }
        [HttpPost("register/organiser")]
        public async Task<IActionResult> RegisterOrganiser(
            RegisterOrganiserRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.FullName) ||
                string.IsNullOrWhiteSpace(request.Email) ||
                string.IsNullOrWhiteSpace(request.Password) ||
                string.IsNullOrWhiteSpace(request.OrganisationName))
            {
                return BadRequest(
                    "Full name, email, password and organisation name are required.");
            }
            if (request.Password.Length < 8)
            {
                return BadRequest("Password must be at least 8 characters.");
            }
            var email = request.Email.Trim().ToLowerInvariant();
            if (await _context.Participants.AnyAsync(p => p.Email == email) ||
                await _context.Organisers.AnyAsync(o => o.Email == email))
            {
                return Conflict("Email is already registered.");
            }
            var organiser = new Organiser
            {
                FullName = request.FullName.Trim(),
                Email = email,
                OrganisationName = request.OrganisationName.Trim()
            };
            organiser.PasswordHash =
                _organiserHasher.HashPassword(
                    organiser, request.Password);
            _context.Organisers.Add(organiser);
            await _context.SaveChangesAsync();
            return Ok(new
            {
                message = "Organiser registered successfully.",
                organiser.OrganiserId,
                organiser.FullName,
                organiser.Email,
                organiser.OrganisationName
            });
        }
        [HttpPost("login")]
        public async Task<IActionResult> Login(LoginRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Email) ||
                string.IsNullOrWhiteSpace(request.Password))
            {
                return BadRequest("Email and password are required.");
            }
            var email = request.Email.Trim().ToLowerInvariant();
            var participant = await _context.Participants
                .FirstOrDefaultAsync(p => p.Email == email);
            if (participant != null &&
                _participantHasher.VerifyHashedPassword(
                    participant,
                    participant.PasswordHash,
                    request.Password) != PasswordVerificationResult.Failed)
            {
                return CreateToken(
                    participant.ParticipantId,
                    participant.Email,
                    "Participant");
            }
            var organiser = await _context.Organisers
                .FirstOrDefaultAsync(o => o.Email == email);
            if (organiser != null &&
                _organiserHasher.VerifyHashedPassword(
                    organiser,
                    organiser.PasswordHash,
                    request.Password) != PasswordVerificationResult.Failed)
            {
                return CreateToken(
                    organiser.OrganiserId,
                    organiser.Email,
                    "Organiser");
            }
            return Unauthorized("Invalid email or password.");
        }
        private IActionResult CreateToken(
            int userId,
            string email,
            string role)
        {
            var key = _configuration["Jwt:Key"];
            var issuer = _configuration["Jwt:Issuer"];
            var audience = _configuration["Jwt:Audience"];
            if (string.IsNullOrWhiteSpace(key) ||
                string.IsNullOrWhiteSpace(issuer) ||
                string.IsNullOrWhiteSpace(audience))
            {
                throw new InvalidOperationException(
                    "JWT configuration is incomplete.");
            }
            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
                new Claim(ClaimTypes.Email, email),
                new Claim(ClaimTypes.Role, role)
            };
            var signingKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(key));
            var credentials = new SigningCredentials(
                signingKey,
                SecurityAlgorithms.HmacSha256);
            var token = new JwtSecurityToken(
                issuer: issuer,
                audience: audience,
                claims: claims,
                expires: DateTime.UtcNow.AddHours(2),
                signingCredentials: credentials);
            return Ok(new
            {
                token = new JwtSecurityTokenHandler().WriteToken(token),
                role,
                userId
            });
        }
    }
}