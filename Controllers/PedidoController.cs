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
                pedido.Status.Id = 1;
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
                    .ThenBy(p => p.Status.Id)
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
                    Status = p.Status.Id
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

        [HttpGet("paginar")]
        public async Task<IActionResult> Paginarloja(uint loja, int indice, int total)
        {
            Mensageiro mensageiro = new Mensageiro(200, "Operação realizada com sucesso");
            try 
            {
                var xx = await Paginar<Pedido>.CreateAsync(_context.Pedidos.Include(p => p.Itens).Include(p => p.Atendente).Include(p => p.FormaPagamento)
                        .Where(p => p.Loja.Id == loja && p.Status.Id == 3 && p.Itens.Count() > 0).AsNoTracking()
                        .OrderBy(p => p.Id), indice, total);
                mensageiro.Dados = xx;
            }
            catch (Exception ex)
            {
                mensageiro.Codigo = 300;
                mensageiro.Mensagem = ex.Message;
            }
            return Ok(mensageiro);

        }

        [HttpGet("obtermes")]
        public async Task<IActionResult> ObterMes(uint loja)
        {
            Mensageiro mensageiro = new Mensageiro(200, "Operação realizada com sucesso");

            try 
            {
                var xx = _context.Pedidos
                .Include(p => p.Itens)
                .Where(p => p.LojaId == loja && p.Data.Value.Month == DateTime.Now.Month && p.Status.Id == 3)
                .AsNoTracking().ToList();
                mensageiro.Dados = xx;
            }
            catch (Exception ex)
            {
                mensageiro.Codigo = 300;
                mensageiro.Mensagem = ex.Message; 
            }

            return Ok(mensageiro);
        }

        [HttpGet("obtersomames")]
        public async Task<IActionResult> ObterSomaMes(uint loja)
        {
            Mensageiro mensageiro = new Mensageiro(200, "Operação realizada com sucesso!");
            try 
            {
                var xx = _context.Pedidos.Include(p => p.Itens)
                .Where(p => p.LojaId == loja && p.Data.Value.Month == DateTime.Now.Month && p.Status.Id == 3)
                .Sum(p => p.Itens.Sum(x => x.Preco * x.Quantidade) + p.Loja.TaxaEntrega);
                mensageiro.Dados = xx;
            }
            catch(Exception ex)
            {
                mensageiro.Codigo = 300;
                mensageiro.Mensagem = ex.Message;
            }
            return Ok(mensageiro);
        }

        [HttpGet("obterdia")]
        public async Task<IActionResult> ObterDia(uint loja) 
        {
            Mensageiro mensageiro = new Mensageiro(200, "Operação realizada com sucesso");
            try 
            {
                var xx = _context.Pedidos
                .Include(p => p.Itens)
                .Where(p => p.LojaId == loja && p.Data.Value.Date == DateTime.Now.Date && p.Status.Id == 3)
                .AsNoTracking().ToList();
                mensageiro.Dados = xx;
            }
            catch(Exception ex)
            {
                mensageiro.Codigo = 300;
                mensageiro.Mensagem = ex.Message;
            }

            return Ok(mensageiro);
        
        }

        [HttpGet("obtersomadia")]
        public async Task<IActionResult> ObterSomaDia(uint loja)
        {
            Mensageiro mensageiro = new Mensageiro(200, "Operação realizada com sucesso");
            try 
            {
                var xx = _context.Pedidos.Include(p => p.Itens)
                .Where(p => p.LojaId == loja && p.Data.Value.Date == DateTime.Now.Date && p.Status.Id == 3)
                .Sum(p => p.Itens.Sum(x => x.Preco * x.Quantidade) + p.Loja.TaxaEntrega); ;
                mensageiro.Dados = xx;
            }
            catch(Exception ex) 
            {
                mensageiro.Codigo = 300;
                mensageiro.Mensagem = ex.Message;
            }

            return Ok(mensageiro);
        }

        [HttpGet("obter2DA")]
        public async Task<IActionResult> ObterPedidos2DiaAnterior(uint loja) 
        {
            Mensageiro mensageiro = new Mensageiro(200, "Operação realizada com sucesos");
            try 
            {
                var xx = _context.Pedidos.Include(p => p.Itens)
                .Where(p => p.LojaId == loja && p.Data.Value.Date == DateTime.Now.AddDays(-2).Date && p.Status.Id == 3)
                .AsNoTracking().ToList();
                mensageiro.Dados = xx;
            }
            catch(Exception ex)
            {
                mensageiro.Codigo = 300;
                mensageiro.Mensagem = ex.Message;
            }

            return Ok(mensageiro);
        }

        [HttpGet("obtersoma2DA")]
        public async Task<IActionResult?> ObterSomaVendas2Dia(uint loja) 
        {
            Mensageiro mensageiro = new Mensageiro(200, "Operação realizada com sucesos");

            try 
            {
                var xx = _context.Pedidos.Include(p => p.Itens)
                .Where(p => p.LojaId == loja && p.Data.Value.Date == DateTime.Now.AddDays(-2).Date && p.Status.Id == 3)
                .Sum(p => p.Itens.Sum(x => x.Preco * x.Quantidade) + p.Loja.TaxaEntrega);
                mensageiro.Dados = xx;
            }
            catch (Exception ex)
            {
                mensageiro.Codigo = 300;
                mensageiro.Mensagem = ex.Message;
            }

            return Ok(mensageiro);
        }

        [HttpGet("obternovosclientes")]
        public async Task<IActionResult> ObterTotalNovosClientes(uint loja)
        {
            Mensageiro mensageiro = new Mensageiro(200, "Operação realizada com sucesso");

            try 
            {
                var usuarios = _context.Pedidos.Where(p => p.LojaId == loja && p.Data.Value.Date == DateTime.Now.AddDays(-2).Date && p.Status.Id == 3)
                .GroupBy(p => p.UsuarioId)
                .Select(p => p.Key).ToList();

                var usuariosAntigos = _context.Pedidos.Where(p => p.LojaId == loja &&
                p.Data.Value.Date < DateTime.Now.AddDays(-2).Date &&
                usuarios.Contains(p.UsuarioId) && p.Status.Id == 3)
                    .GroupBy(p => p.UsuarioId)
                    .Select(p => p.Key).ToList();

                mensageiro.Dados = usuarios.Except(usuariosAntigos).Count();
            }
            catch (Exception ex)
            {
                mensageiro.Codigo = 300;
                mensageiro.Mensagem = ex.Message; 
            }

            return Ok(mensageiro);

        }

        [HttpGet("obtersomanovosclientes")]
        public async Task<IActionResult> ObterSomaNovosClientes(uint loja) 
        {
            Mensageiro mensageiro = new Mensageiro(200, "Operação realizada com sucesso!");

            try 
            {
                //busca os pedidos de 2 dias atrás
                var usuarios = _context.Pedidos.Where(p => p.LojaId == loja && p.Data.Value.Date == DateTime.Now.AddDays(-2).Date)
                    .GroupBy(p => p.UsuarioId)
                    .Select(p => p.Key).ToList();
                //buscar todos os pedidos realizados anteriormente 
                var usuariosAntigos = _context.Pedidos.Where(p => p.LojaId == loja && p.Data.Value.Date < DateTime.Now.AddDays(-2).Date && usuarios.Contains(p.UsuarioId))
                    .GroupBy(p => p.UsuarioId)
                    .Select(p => p.Key).ToList();
                //pega somente os usuários que não fizeram pedidos anteriormente
                var novos = usuarios.Except(usuariosAntigos);

                mensageiro.Dados = _context.Pedidos.Include(p => p.Itens)
                    .Where(p => p.LojaId == loja && p.Data.Value.Date == DateTime.Now.AddDays(-2).Date && novos.Contains(p.UsuarioId))
                    .Sum(p => p.Itens.Sum(q => q.Quantidade * q.Preco) + p.Loja.TaxaEntrega);
            }
            catch (Exception ex)
            {
                mensageiro.Codigo = 300;
                mensageiro.Mensagem = ex.Message;
            }

            return Ok(mensageiro);
        }

        //[HttpGet("obterprodutomaisvendido")]
        //public async Task<IActionResult> ObterProdutoMaisVendido(uint loja) 
        //{
        //    Mensageiro mensageiro = new Mensageiro(200, "Operação realizada com sucesso");

        //    try 
        //    {
        //        var grp = _context.PedidoItens
        //                           .Include(p => p.Pedido)
        //                           .Include(p => p.Produto)
        //                           .Where(p => p.Pedido.LojaId == loja && p.Pedido.Data.Value.Date == DateTime.Now.AddDays(-2).Date && p.Pedido.Status.Id == 3)
        //                           .ToList()
        //                           .GroupBy(p => p.Produto);

        //        mensageiro.Dados = grp.Select(p => new { Produto = p.Key, Total = p.Sum(x => x.Quantidade) })
        //                    .OrderByDescending(p => p.Total)
        //                    .FirstOrDefault().Produto;
        //    }
        //    catch (Exception ex)
        //    {
        //        mensageiro.Codigo = 300;
        //        mensageiro.Mensagem = ex.Message;
        //    }
           
        //    return Ok(mensageiro);
        //}
       
        [HttpGet("obtersemana")]
        public async Task<IActionResult> ObterPedidosSemana(uint loja)
        {
            Mensageiro mensageiro = new Mensageiro(200, "Operação realizada com sucesso");
            try 
            {
                var xx = _context.Pedidos.Include(p => p.Itens)
                .Where(p => p.LojaId == loja && p.Data.Value.Date > DateTime.Now.AddDays(-7).Date && p.Status.Id == 3)
                .AsNoTracking().ToList();
                mensageiro.Dados = xx;
            }
            catch (Exception ex) 
            {
                mensageiro.Codigo = 300;
                mensageiro.Mensagem = ex.Message;
            }
            return Ok(mensageiro);
        }

        [HttpGet("obtersomasemana")]
        public async Task<IActionResult> ObterSomaVendasSemana(uint loja, decimal taxa) 
        {
            Mensageiro mensageiro = new Mensageiro(200, "Operação realizada com sucesso!");

            try 
            {

                var soma = _context.Pedidos.Include(p => p.Itens)
                    .Where(p => p.LojaId == loja && p.Data.Value.Date > DateTime.Now.AddDays(-7).Date && p.Status.Id == 3)
                    .Sum(p => p.Itens.Sum(x => x.Preco * x.Quantidade) + taxa);

                mensageiro.Dados = soma;
            }
            catch (Exception ex)
            {
                mensageiro.Codigo = 300;
                mensageiro.Mensagem = ex.Message;
            }
            return Ok(mensageiro);
        }

        [HttpGet("obterpedido7D")]
        public async Task<IActionResult> ObterPedidosUltimos7Dias(uint loja, decimal taxa) 
        {
            Mensageiro mensageiro = new Mensageiro(200, "Operação realizada com sucesso!");

            try 
            {
                decimal[] valores = new decimal[7];

                var dias = _context.Pedidos
                    .Include(p => p.Itens)
                    .Where(p => p.LojaId == loja && p.Data.Value.Date > DateTime.Now.AddDays(-7).Date && p.Status.Id == 3)
                    .AsNoTracking()
                    .ToList()
                    .GroupBy(p => new { DiaSemana = p.Data.Value.DayOfWeek })
                    .OrderBy(p => p.Key.DiaSemana);



                for (var i = 0; i < 7; i++)
                {
                    var grp = dias.FirstOrDefault(p => (int)p.Key.DiaSemana == i);
                    if (grp != null)
                        valores[i] = grp.Sum(p => p.Itens.Sum(x => x.Quantidade.Value * x.Preco.Value) + taxa);
                    else
                        valores[i] = 0;
                }

                mensageiro.Dados = valores;
            }
            catch(Exception ex) 
            {
                mensageiro.Codigo = 300;
                mensageiro.Mensagem = ex.Message;
            }
            return Ok(mensageiro);
        }

        [HttpGet("obterpedido14D")]
        public async Task<IActionResult> ObterPedidos14Dias(uint loja, decimal taxa) 
        {
            Mensageiro mensageiro = new Mensageiro(200, "Operação realizada com sucesso");

            try 
            {
                decimal[] valores = new decimal[7];

                var dias = _context.Pedidos
                    .Include(p => p.Itens)
                    .Where(p => p.LojaId == loja && p.Data.Value.Date <= DateTime.Now.AddDays(-7).Date && p.Data.Value.Date > DateTime.Now.AddDays(-14).Date && p.Status.Id == 3)
                    .AsNoTracking()
                    .ToList()
                    .GroupBy(p => new { DiaSemana = p.Data.Value.DayOfWeek })
                    .OrderBy(p => p.Key.DiaSemana);


                for (var i = 0; i < 7; i++)
                {
                    var grp = dias.FirstOrDefault(p => (int)p.Key.DiaSemana == i);
                    if (grp != null)
                        valores[i] = grp.Sum(p => p.Itens.Sum(x => x.Quantidade.Value * x.Preco.Value) + taxa);
                    else
                        valores[i] = 0;
                }

                mensageiro.Dados = valores;
            }
            catch(Exception ex)
            {
                mensageiro.Codigo = 300;
                mensageiro.Mensagem = ex.Message;
            }
            return Ok(mensageiro);
        }

        [HttpGet("atrasados")]
        public async Task<IActionResult> PorcentagemAtraso(uint loja)
        {
            Mensageiro mensageiro = new Mensageiro(200, "Operação realizada com sucesso");
            try 
            {
                var entregasAtrasos = _context.Pedidos.Where(p => p.DataEntrega.Value > p.Data.Value.AddMinutes(p.Loja.TempoMaximo.Value)
                 && p.LojaId == loja && p.Status.Id == 3 && p.Data.Value.Date > DateTime.Now.AddDays(-7).Date)
               .GroupBy(p => p.Id)
               .Select(p => p.Key).ToList();

                mensageiro.Dados = entregasAtrasos.Count();
            }
            catch (Exception ex)
            {
                mensageiro.Codigo = 300;
                mensageiro.Mensagem = ex.Message;
            }

            return Ok(mensageiro);
        }
    }
}
