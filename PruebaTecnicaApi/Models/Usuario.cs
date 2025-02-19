using System;
using System.Collections.Generic;

namespace PruebaTecnicaApi.Models;

public partial class Usuario
{
    public int Id { get; set; }

    public string Nombre { get; set; } = null!;

    public string Correo { get; set; } = null!;

    public string Contraseña { get; set; } = null!;

    public DateTime? FechaRegistro { get; set; }
}
