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
