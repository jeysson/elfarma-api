using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AllDelivery.Lib;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AllDelivery.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ProdutoController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public ProdutoController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet("todos")]
        public IEnumerable<Produto> Todos(int loja)
        {
            return _context.Produtos.Include(p=> p.GrupoProdutos).ThenInclude(p=> p.Grupo).Where(p => p.GrupoProdutos.Count > 0 && p.Ativo && p.Loja.Id == loja);
        }

        [HttpGet("grupos")]
        public IEnumerable<Grupo> Grupo(int loja)
        {
            return _context.Grupos.Include(p=> p.GrupoProdutos).Where(p => p.GrupoProdutos.Count > 0 && p.Loja.Id == loja);
        }

        [HttpGet("produtosgrupo")]
        public IEnumerable<Produto> produtosgrupo(int loja, int grupo)
        {
            return _context.Produtos.Include(p => p.GrupoProdutos).ThenInclude(p => p.Grupo).Where(p => p.GrupoProdutos.Count(z=> z.GrupoId == grupo) > 0 && p.Loja.Id == loja);
        }

        [HttpGet("imagens")]
        public IEnumerable<ProdutoFoto> Imagens(int produto)
        {
            var list = _context.ProdutoFotos.Where(p => p.ProdutoId == produto).ToList();
            list.ForEach(o=> { o.FotoBase64 = Convert.ToBase64String(o.Foto); });
            return list;
        }

        [HttpGet("paginar")]
        public async Task<Paginar<Produto>> Paginar(int loja, int grupo, int indice, int tamanho)
        {
            if(grupo == -1)
                return await Paginar<Produto>.CreateAsync(_context.Produtos.Where(p => p.Loja.Id == loja), indice, tamanho);
            else
            return await Paginar<Produto>.CreateAsync(_context.Produtos.Where(p=> p.Loja.Id == loja && p.GrupoProdutos.Count(p=> p.GrupoId == grupo )> 0),  indice, tamanho);
        }
    }
}
