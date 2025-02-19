using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using PruebaTecnicaApi.Models;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace PruebaTecnicaApi.Controllers
{

    [Route("api/[controller]")]
    [ApiController]
    public class LoginController : ControllerBase
    {
        private readonly TiendaContext _context;
        private readonly IConfiguration _config;
        public LoginController(TiendaContext context, IConfiguration configuration)
        {
            _context = context;
            _config = configuration;
        }
        [HttpPost("UserLogin")]
        public async Task<IActionResult> Login(UserLogin userLogin)
        {
            var user = await Authenticate(userLogin);
            if (user != null)
            {
                //crear el token
                var jwtToken = GenerateToken(user);
                return Ok(jwtToken);
            }
            return NotFound("Usuario o Contraseña incorrectos");
        }

        private async Task<Usuario> Authenticate(UserLogin userLogin)
        {
            //buscar el usuario en la base de datos
            var user = await _context.Usuarios.FirstOrDefaultAsync(u => u.Correo == userLogin.Correo && u.Contraseña == userLogin.Contraseña);
            return user;
        }
        private string GenerateToken(Usuario usuario)
        {
            var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(s: _config["Jwt:Key"]));
            var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);
            var clains = new[]
            {
                new Claim(ClaimTypes.NameIdentifier,usuario.Correo),
                new Claim(ClaimTypes.GivenName,usuario.Nombre),
            };

            var token = new JwtSecurityToken(
                issuer: _config["Jwt:Issuer"],
                audience: _config["Jwt:Audience"],
                clains,
                expires: DateTime.Now.AddMinutes(30),
                signingCredentials: credentials
            );
            //generar el token
            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}
