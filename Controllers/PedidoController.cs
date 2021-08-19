using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AllDelivery.Lib;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NetTopologySuite.Geometries;

namespace AllDelivery.Api.Controllers
{
    [ApiController]
    [Authorize("Bearer")]
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
                mensageiro.Dados = pedido.Id;

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
        public IActionResult Obter(uint idPedido)
        {
            Mensageiro mensageiro = new Mensageiro(200, "Operação realizada com sucesso!");
            try
            {
                var pedido = _context.Pedidos.Where(p => p.Id == idPedido)
                     .Include(p => p.Itens).ThenInclude(p => p.Produto)
                                                     .Include(p => p.FormaPagamento)
                                                     .Include(p => p.Loja).FirstOrDefault();
                pedido.HistStatus = _context.HistoricoPedidos.Include(p => p.Status).Where(p => p.Pedido.Id == idPedido)
                                                                .Select(p => new StatusPedido
                                                                {
                                                                    Ativo = p.Status.Ativo,
                                                                    Descricao = p.Status.Descricao,
                                                                    Id = p.Status.Id,
                                                                    Nome = p.Status.Nome,
                                                                    Sequencia = p.Status.Sequencia
                                                                }).OrderByDescending(p=> p.Id).ToList();
                mensageiro.Dados = pedido;
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

                var page = await Paginar<Pedido>.CreateAsync(_context.Pedidos
                    .Where(p => p.UsuarioId == codUser)
                     .Include(p => p.Loja)
                    .Include(p => p.Itens)
                    .ThenInclude(p => p.Produto)
                    .OrderByDescending(p => p.Data)
                    .ThenBy(p => p.StatusId)
                    , indice, tamanho);

                object list = new Paginar<Object>(page.Itens.Select(p => new
                {
                    Id = p.Id,
                    Loja = p.Loja.NomeFantasia,
                    Logo = p.Loja.ImgLogo,
                    Data = p.Data,
                    NomeItem = p.Itens.First().Produto.Nome,
                    QuantidadeItem = p.Itens.First().Quantidade,
                    Quantidade = p.Itens.Count,
                    Status = p.StatusId
                }).ToList<object>(), page.Itens.Count, indice, tamanho);

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

        [HttpGet("obteravaliacaopendente")]
        public async Task<Mensageiro> ObterAvaliacaoPendente(int codUser)
        {
            Mensageiro mensageiro = new Mensageiro(200, "Operação realizada com sucesso!");
            try
            {
                var dt = DateTime.Now.AddDays(-16);

                var list = _context.Pedidos.Include(p => p.Loja).Where(p => p.UsuarioId == codUser
                                                              && p.Data.Value.Date > dt.Date
                                                              && !_context.PedidoAvaliacoes.Any(q => q.PedidoId == p.Id));

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
