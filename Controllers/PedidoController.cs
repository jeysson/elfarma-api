using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AllDelivery.Lib;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
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
                var loja = pedido.Loja;
                var fp = pedido.FormaPagamento;

                _context.Database.BeginTransaction();
                pedido.Data = DateTime.UtcNow;
                pedido.StatusId = 1;
                pedido.Ende = pedido.Endereco.ToString();
                pedido.LojaId = pedido.Loja.Id;
                pedido.Loja = null;
                pedido.FormaPagamentoId = pedido.FormaPagamento.Id;
                pedido.FormaPagamento = null;
                pedido.Location = new Point(pedido.Endereco.Lat, pedido.Endereco.Longi);
                //
                pedido.Itens.ForEach(o =>
                {
                    o.ProdutoId = o.Produto.Id;
                    o.Produto = null;
                });
                //
                _context.Pedidos.Add(pedido);
                _context.SaveChanges();
                _context.Database.CommitTransaction();
                //
                pedido.FormaPagamento = fp;
                pedido.Loja = loja;
                //
                mensageiro.Dados = pedido;

            }catch(Exception ex)
            {
                _context.Database.RollbackTransaction();
                mensageiro.Codigo = 300;
                mensageiro.Mensagem = "Falha na operação!";
                mensageiro.Dados = new { message = ex.Message, stack = ex.StackTrace,
                innerMessage = ex.InnerException != null? ex.InnerException.Message: null,
                innerstack = ex.InnerException != null ? ex.InnerException.StackTrace: null
                };
            }

            return Ok(mensageiro);
        }

        [HttpGet("obter")]
        public IActionResult Obter(uint idpedido)
        {
            Mensageiro mensageiro = new Mensageiro(200, "Operação realizada com sucesso!");
            try
            {                
                mensageiro.Dados = _context.Pedidos.Include(p=> p.Itens).ThenInclude(p=> p.Produto)
                                                    .Include(p => p.FormaPagamento)
                                                    .Include(p => p.Loja)
                                                    .FirstOrDefault(p=> p.Id == idpedido);
            }
            catch (Exception ex)
            {
                _context.Database.RollbackTransaction();
                mensageiro.Codigo = 300;
                mensageiro.Mensagem = "Falha na operação!";
            }

            return Ok(mensageiro);
        }

        [HttpGet("obterhistorico")]
        public async Task<Mensageiro> ObterHistorico(uint codUser, int indice, int tamanho)
        {
            Mensageiro mensageiro = new Mensageiro(200, "Operação realizada com sucesso!");
            try
            {

                var page =await Paginar<Pedido>.CreateAsync( _context.Pedidos
                    .Where(p => p.UsuarioId == codUser)
                     .Include(p => p.Loja)
                    .Include(p => p.Itens)
                    .ThenInclude(p => p.Produto)                    
                    , indice, tamanho);

                object list = new Paginar<Object>(page.Select(p => new 
                {
                    Id = p.Id,
                    Loja = p.Loja.NomeFantasia,
                    Logo = p.Loja.ImgLogo,
                    Data = p.Data,
                    NomeItem = p.Itens.First().Produto.Nome,
                    Quantidade = p.Itens.Count
                }).ToList<object>(), page.Count, indice, tamanho);

                mensageiro.Dados = list;
            }
            catch (Exception ex)
            {
                _context.Database.RollbackTransaction();
                mensageiro.Codigo = 300;
                mensageiro.Mensagem = "Falha na operação!";
            }

            return mensageiro;
        }
    }
}
