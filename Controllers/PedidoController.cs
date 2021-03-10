using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AllDelivery.Lib;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using NetTopologySuite.Geometries;

namespace AllDelivery.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class PedidoController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public PedidoController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpPost("registrar")]
        public IActionResult Registrar(Pedido pedido)
        {
            Mensageiro mensageiro = new Mensageiro(200, "Operação realizada com sucesso!");
            try
            {
                _context.Database.BeginTransaction();
                pedido.Data = DateTime.UtcNow;
                pedido.StatusId = 1;
                _context.Pedidos.Add(pedido);
                _context.SaveChanges();
                _context.Database.CommitTransaction();

            }catch(Exception ex)
            {
                _context.Database.RollbackTransaction();
                mensageiro.Codigo = 300;
                mensageiro.Mensagem = "Falha na operação!";
            }

            return Ok(mensageiro);
        }

        
    }
}
