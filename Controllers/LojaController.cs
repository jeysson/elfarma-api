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
    public class LojaController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public LojaController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet("obterloja")]
        public async Task<IActionResult> ObterLoja(uint loja) 
        {
            Mensageiro mensageiro = new Mensageiro(200, "Operação realizada com sucesso!");
            try 
            {
                //var xx = _context.Lojas.Select(p => new Loja
                //{
                //    Id = p.Id,
                //    CNPJ = p.CNPJ,
                //    NomeRazao = p.NomeRazao,
                //    NomeFantasia = p.NomeFantasia,
                //    Ativo = p.Ativo,
                //    Email = p.Email,
                //    TelefoneCelular = p.TelefoneCelular,
                //    TelefoneAlternativo = p.TelefoneAlternativo,
                //    TelefoneComercial = p.TelefoneComercial,
                //    HAbre = p.HAbre,
                //    HFecha = p.HFecha,
                //    PedidoMinimo = p.PedidoMinimo,
                //    TempoMaximo = p.TempoMaximo,
                //    TempoMinimo = p.TempoMinimo,
                //    Contato = p.Contato,
                //    CEP = p.CEP,
                //    Complemento = p.Complemento,
                //    UF = p.UF,
                //    Bairro = p.Bairro,
                //    Numero = p.Numero,
                //    Descricao = p.Descricao,
                //    Cidade = p.Cidade,
                //    TaxaEntrega = p.TaxaEntrega,
                //    Endereco = p.Endereco
                //}).FirstOrDefault(p => p.Id == loja);
                var xx = _context.Lojas.FirstOrDefault(p => p.Id == loja);
                mensageiro.Dados = xx;
            }
            catch (Exception ex)
            {
                mensageiro.Codigo = 300;
                mensageiro.Mensagem = ex.Message;
            }

            return Ok(mensageiro);
        }

        [HttpPost("cadastrar")]
        public async Task<IActionResult> Cadastrar(Loja loja) 
        {
            Mensageiro mensageiro = new Mensageiro(200 ,"Loja cadastrada com sucesso!");
            try
            {
                _context.Database.BeginTransaction();
                _context.Lojas.Add(loja);
                _context.SaveChanges();
                _context.Database.CommitTransaction();
            }
            catch (Exception ex)
            {
                mensageiro.Codigo = 300;
                mensageiro.Mensagem = "Falha ao cadastrar a loja!";
                _context.Database.RollbackTransaction();
            }
            return Ok(mensageiro);
        }

        [HttpDelete("excluir")]
        public async Task<IActionResult> Excluir(uint loja)
        {
            Mensageiro mensageiro = new Mensageiro(200 ,"Loja excluída com sucesso!");
            try
            {
                _context.Database.BeginTransaction();
                var cc = _context.Lojas.Local.FirstOrDefault(p => p.Id == loja);
                if (cc != null)
                    _context.Entry<Loja>(cc).State = EntityState.Detached;
                _context.Lojas.Remove(cc);
                _context.SaveChanges();
                _context.Database.CommitTransaction();
            }
            catch
            {
                mensageiro.Codigo = 300;
                mensageiro.Mensagem = "Falha ao excluir a loja!";
                _context.Database.RollbackTransaction();
            }
            return Ok(mensageiro);
        }

        [HttpPut("atualizar")]
        public async Task<IActionResult> Atualizar(Loja loja) 
        {
            Mensageiro mensageiro = new Mensageiro(200, "Loja atualizada com sucesso!");
            try
            {
                _context.Database.BeginTransaction();
                var cc = _context.Lojas.Local.FirstOrDefault(p => p.Id == loja.Id);
                if (cc != null)
                    _context.Entry<Loja>(cc).State = EntityState.Detached;
                _context.Lojas.Update(loja);
                _context.SaveChanges();
                _context.Database.CommitTransaction();
            }
            catch(Exception ex)
            {
                mensageiro.Codigo = 300;
                mensageiro.Mensagem = ex.Message;
                _context.Database.RollbackTransaction();
            }
            return Ok(mensageiro);
        }

        [HttpPut("inativar")]
        public async Task<IActionResult> Inativar(Loja loja)
        {
            Mensageiro mensageiro = new Mensageiro(200, string.Format("{0} com sucesso!", loja.Ativo ? "ativada" : "inativada"));
            try
            {
                _context.Database.BeginTransaction();
                _context.Attach(loja);
                _context.Entry<Loja>(loja).Property(p => p.Ativo).IsModified = true;
                _context.Entry<Loja>(loja).Property(p => p.Disponivel).IsModified = true;
                _context.SaveChanges();
                _context.Database.CommitTransaction();
            }
            catch
            {
                mensageiro.Codigo = 300;
                mensageiro.Mensagem = "Falha ao realizar a operação!";
                _context.Database.RollbackTransaction();
            }
            return Ok(mensageiro);
        }

        [HttpGet("paging")]
        public async Task<IActionResult> Paging(int indice, int total) 
        {
            Mensageiro mensageiro = new Mensageiro(200, "Operação realizada com sucesso!");
            try
            {
                mensageiro.Dados = await Paginar<Loja>.CreateAsync(_context.Lojas.OrderBy(p => p.Id).Select(p => new Loja
               {
                   Id = p.Id,
                   CNPJ = p.CNPJ,
                   NomeRazao = p.NomeRazao,
                   NomeFantasia = p.NomeFantasia,
                   Disponivel = p.Disponivel,
                   Ativo = p.Ativo,
                   Email = p.Email
               }), indice, total);
                
            }
            catch (Exception ex) 
            {
                mensageiro.Codigo = 300;
                mensageiro.Mensagem = ex.Message;
            }
            return Ok(mensageiro);
        }

        [HttpGet("ativas")]
        public IEnumerable<Loja> Ativas(double lat, double lon, TipoOrdenacao tipoOrdenacao)
        {
            List<Loja> valores = null;
            //
            switch (tipoOrdenacao)
            {
                case TipoOrdenacao.Distancia:
                    valores = _context.Lojas.OrderBy(p => p.NomeFantasia).ToList();
                    break;
                case TipoOrdenacao.TempoEntrega:
                    valores = _context.Lojas.OrderBy(p => p.TempoMinimo).ToList();
                    break;
                case TipoOrdenacao.TaxaEntrega:
                    valores = _context.Lojas.OrderBy(p => p.TaxaEntrega).ToList();
                    break;
                case TipoOrdenacao.OrdemAZ:
                default:
                    valores = _context.Lojas.OrderBy(p => p.NomeFantasia).ToList();
                    break;
            }
            //
            if (valores != null)
                valores.ForEach(o =>
                {
                    o.Referencia = new Point(lat, lon) { SRID = 2855 };
                });
            return valores;
        }

        [HttpGet("paginar")]
        public async Task<Paginar<Loja>> Paginar(int indice, int tamanho,double lat, double lon, TipoOrdenacao tipoOrdenacao)
        {
            Paginar<Loja> valores = null;
            //
            switch (tipoOrdenacao)
            {             
                case TipoOrdenacao.Distancia:
                    valores = await Paginar<Loja>.CreateAsync(_context.Lojas.OrderBy(p => p.Location.Distance(new Point(lon, lat))).Select(p=> new Loja { 
                        Id = p.Id,
                       // ImgLogo = p.ImgLogo,
                      //  ImgBanner = p.ImgBanner,
                        NomeRazao = p.NomeRazao,
                        NomeFantasia = p.NomeFantasia,
                        HAbre = p.HAbre,
                        HFecha = p.HFecha,
                        TaxaEntrega = p.TaxaEntrega,
                        TempoMaximo = p.TempoMaximo,
                        TempoMinimo = p.TempoMinimo,
                        Disponivel = p.Disponivel,
                        Ativo = p.Ativo,
                        Distancia = string.Format("{0:n2} km", p.Location.Distance(new Point(lon, lat))/1000),
                        PedidoMinimo = p.PedidoMinimo
                    } ), indice, tamanho);
                    break;
                case TipoOrdenacao.TempoEntrega:
                    valores = await Paginar<Loja>.CreateAsync(_context.Lojas.OrderBy(p => p.TempoMinimo).Select(p => new Loja
                    {
                        Id = p.Id,
                       // ImgLogo = p.ImgLogo,
                       // ImgBanner = p.ImgBanner,
                        NomeRazao = p.NomeRazao,
                        NomeFantasia = p.NomeFantasia,
                        HAbre = p.HAbre,
                        HFecha = p.HFecha,
                        TaxaEntrega = p.TaxaEntrega,
                        TempoMaximo = p.TempoMaximo,
                        TempoMinimo = p.TempoMinimo,
                        Disponivel = p.Disponivel,
                        Ativo = p.Ativo,
                        Distancia = string.Format("{0:n2} km", p.Location.Distance(new Point(lon, lat)) / 1000),
                        PedidoMinimo = p.PedidoMinimo
                    }), indice, tamanho);
                    break;
                case TipoOrdenacao.TaxaEntrega:
                    valores = await Paginar<Loja>.CreateAsync(_context.Lojas.OrderBy(p => p.TaxaEntrega).Select(p => new Loja
                    {
                        Id = p.Id,
                        //ImgLogo = p.ImgLogo,
                       // ImgBanner = p.ImgBanner,
                        NomeRazao = p.NomeRazao,
                        NomeFantasia = p.NomeFantasia,
                        HAbre = p.HAbre,
                        HFecha = p.HFecha,
                        TaxaEntrega = p.TaxaEntrega,
                        TempoMaximo = p.TempoMaximo,
                        TempoMinimo = p.TempoMinimo,
                        Disponivel = p.Disponivel,
                        Ativo = p.Ativo,
                        Distancia = string.Format("{0:n2} km", p.Location.Distance(new Point(lon, lat)) / 1000),
                        PedidoMinimo = p.PedidoMinimo
                    }), indice, tamanho);
                    break;
                case TipoOrdenacao.OrdemAZ:
                default:
                    valores = await Paginar<Loja>.CreateAsync(_context.Lojas.OrderBy(p => p.NomeFantasia).Select(p => new Loja
                    {
                        Id = p.Id,
                        //ImgLogo = p.ImgLogo,
                       // ImgBanner = p.ImgBanner,
                        NomeRazao = p.NomeRazao,
                        NomeFantasia = p.NomeFantasia,
                        HAbre = p.HAbre,
                        HFecha = p.HFecha,
                        TaxaEntrega = p.TaxaEntrega,
                        TempoMaximo = p.TempoMaximo,
                        TempoMinimo = p.TempoMinimo,
                        Disponivel = p.Disponivel,
                        Ativo = p.Ativo,
                        Distancia = string.Format("{0:n2} km", p.Location.Distance(new Point(lon, lat)) / 1000),
                        PedidoMinimo = p.PedidoMinimo
                    }), indice, tamanho);
                    break;
            }
           
            return valores;
        }

        [HttpGet("logo")]
        public IActionResult Logo(int loja)
        {
            var lj = _context.Lojas.FirstOrDefault(p => p.Id == loja);
            try
            {
                return Ok(new { Id = lj.Id, Logo = Convert.ToBase64String(lj.ImgLogo) });
            }
            catch {
                return Ok(new { Id = lj.Id });
            }
        }
        [HttpGet("banner")]
        public IActionResult Banner(int loja)
        {
            var lj = _context.Lojas.FirstOrDefault(p => p.Id == loja);
            try
            {
                return Ok(new { Id = lj.Id, Banner = Convert.ToBase64String(lj.ImgBanner) });
            }
            catch
            {
                return Ok(new { Id = lj.Id});
            }
        }
        [HttpGet("formaspagamento")]
        public IActionResult FormasPagamento(int loja)
        {
            var formas = _context.LojaFormaPagamentos.Include(p=> p.Loja)
                                                 .Include(p=> p.FormaPagamento)
                                                 .Where(p => p.Loja.Id == loja)
                                                 .Select(p=> p.FormaPagamento);
            return Ok(formas);
        }
    }
}
