using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using PruebaTecnicaApi.Models;

namespace PruebaTecnicaApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class UsuariosController : Controller
    {
        private readonly TiendaContext _context;

        public UsuariosController(TiendaContext context)
        {
            _context = context;
        }

        //crear usuarios
        [HttpPost("NewUsuario")]
        public async Task<ActionResult<Usuario>> PostUsuario(Usuario usuario)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }
            var hashedPassword = HashPassword(usuario.Contraseña);
            var newUsuario = new Usuario
            {
                Nombre = usuario.Nombre,
                Correo = usuario.Correo,
                Contraseña = hashedPassword,
                FechaRegistro = DateTime.Now
            };
            _context.Usuarios.Add(newUsuario);
            try
            {
                await _context.SaveChangesAsync();
                return CreatedAtAction(nameof(GetUsuario), new { id = newUsuario.Id }, newUsuario);
            }
            catch (DbUpdateException ex) when (ex.InnerException is PostgresException pgEx && pgEx.SqlState == "23505")
            {
                return BadRequest("Este correo ya ha sido usado.");
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = "Ocurrió un error al procesar la solicitud.", details = ex.Message });
            }

        }
        [HttpGet("GetUser/{id}")]
        public async Task<ActionResult<Usuario>> GetUsuario(int id)
        {
            var usuario = await _context.Usuarios.FindAsync(id);

            if (usuario == null)
            {
                return NotFound();
            }

            return usuario;
        }

        private string HashPassword(string password)
        {
            using (var sha256 = SHA256.Create())
            {
                var bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
                var builder = new StringBuilder();
                foreach (var b in bytes)
                {
                    builder.Append(b.ToString("x2"));
                }
                return builder.ToString();
            }
        }

        private bool UsuarioExists(int id)
        {
            return _context.Usuarios.Any(e => e.Id == id);
        }
    }
}
