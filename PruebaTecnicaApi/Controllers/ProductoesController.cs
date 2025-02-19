using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.Web.CodeGenerators.Mvc.Templates.Blazor;
using PruebaTecnicaApi.Models;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace PruebaTecnicaApi.Controllers
{

    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class ProductoesController : Controller
    {
        private readonly TiendaContext _context;

        public ProductoesController(TiendaContext context)
        {
            _context = context;
        }

        // GET: Productoes
        [HttpGet("GetAllProductos")]
        public async Task<ActionResult<IEnumerable<Producto>>> GetAllProductos(int pageNumber = 1, int pageSize = 10, string filtro = null)
        {
            if (pageNumber <= 0) pageNumber = 1;
            if (pageSize <= 0) pageSize = 10;
            var query = _context.Productos.AsQueryable();
            if (!String.IsNullOrEmpty(filtro))
            {
                query = query.Where(p => p.Nombre.Contains(filtro) || p.Descripcion.Contains(filtro));
            }
            var totalCount = await query.CountAsync();
            var productos = await query
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
            return Ok(new
            {
                totalCount,
                pageNumber,
                pageSize,
                productos
            });
        }

        //buscar por id
        [HttpGet("GetProducto/{id}")]
        public async Task<ActionResult<Producto>> GetProducto(int id)
        {
            if(id <= 0)
            {
                return BadRequest("El ID debe ser un valor positivo");
            }
            var producto = await _context.Productos.FindAsync(id);
            if (producto == null)
            {
                return NotFound();
            }
            return producto;
        }
        //agregar un producto
        [HttpPost("NewProducto")]
        public async Task<ActionResult<Producto>> PostProducto(Producto producto)
        {
            if (!ModelState.IsValid) {
                return BadRequest(ModelState);
            }
            try
            {
                _context.Productos.Add(producto);
                await _context.SaveChangesAsync();
                return CreatedAtAction("GetProducto", new { id = producto.Id }, producto);
            }
            catch (DbUpdateException dbEx) {
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = "Error al agregar el producto.", details = dbEx.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = "Ocurrio un error al procesar la solicitud.", details = ex.Message });
            }
           
        }

        //update de un producto
        [HttpPut("UpdateProducto/{id}")]
        public async Task<IActionResult> PutProducto(int id, Producto producto)
        {
            if(id <= 0)
            {
                return BadRequest("El ID debe ser un valor positivo");
            }
            if (id != producto.Id)
            {
                return BadRequest("El ID del producto no coincide");
            }
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }
            _context.Entry(producto).State = EntityState.Modified;
            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!ProductoExists(id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }
            catch (DbUpdateException dbEx)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = "Error al actualizar el producto.", details = dbEx.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = "Ocurrió un error al procesar la solicitud.", details = ex.Message });
            }
            return NoContent();
        }
        //eleminar un producto
        [HttpDelete("DeleteProducto/{id}")]
        public async Task<IActionResult> DeleteProducto(int id)
        {
            if (id <= 0)
            {
                return BadRequest("El ID debe ser un valor positivo.");
            }
            try 
            {
                var producto = await _context.Productos.FindAsync(id);
                if (producto == null)
                {
                    return NotFound();
                }
                _context.Productos.Remove(producto);
                await _context.SaveChangesAsync();
                return NoContent();

            } catch (DbUpdateException dbEx) 
            {
                return  StatusCode(StatusCodes.Status500InternalServerError,new {message= "error al eliminar el producto.",details=dbEx.Message});
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = "Ocurrio un error al procesar la solicitud.", details = ex.Message });
            }

        }
        [HttpGet("GeneratedPdfReport")]
        public async Task<IActionResult> GeneratedPdfReport()
        {
            QuestPDF.Settings.License = LicenseType.Community;
            var productos = await _context.Productos.ToListAsync();
            var document = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(2, Unit.Centimetre);
                    page.Header()
                        .Text("Reporte de Productos")
                        .FontSize(20)
                        .Bold()
                        .AlignCenter();
                    page.Content()
                        .Table(table =>
                        {
                            table.ColumnsDefinition(columns =>
                            {
                                columns.RelativeColumn();
                                columns.RelativeColumn();
                                columns.RelativeColumn();
                                columns.RelativeColumn();
                            });
                            table.Header(header =>
                            {
                                header.Cell().Element(CellStyle).Text("ID");
                                header.Cell().Element(CellStyle).Text("Nombre");
                                header.Cell().Element(CellStyle).Text("Descripción");
                                header.Cell().Element(CellStyle).Text("Precio");

                                static IContainer CellStyle(IContainer container)
                                {
                                    return container.DefaultTextStyle(x => x.Bold()).PaddingVertical(5).BorderBottom(1).BorderColor(Colors.Black);
                                }
                            });
                            foreach (var producto in productos)
                            {
                                table.Cell().Element(CellStyle).Text(producto.Id.ToString());
                                table.Cell().Element(CellStyle).Text(producto.Nombre);
                                table.Cell().Element(CellStyle).Text(producto.Descripcion);
                                table.Cell().Element(CellStyle).Text(producto.Precio.ToString("C"));
                            }

                            static IContainer CellStyle(IContainer container)
                            {
                                return container.BorderBottom(1).BorderColor(Colors.Grey.Lighten2).PaddingVertical(5);
                            }
                        });
                });

            });
            var pdfStream = new MemoryStream();
            document.GeneratePdf(pdfStream);
            pdfStream.Position = 0;
            return File(pdfStream, "application/pdf", "ReporteProductos.pdf");
        }

        private bool ProductoExists(int id)
         {
             return _context.Productos.Any(e => e.Id == id);
         }
    }
}
