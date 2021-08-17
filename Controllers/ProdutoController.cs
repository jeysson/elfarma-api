using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AllDelivery.Lib;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AllDelivery.Api.Controllers
{
    [ApiController]
    [Authorize("Bearer")]    
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
            return _context.Produtos.Include(p=> p.GrupoProdutos)
                .ThenInclude(p=> p.Grupo)
                .Where(p => p.GrupoProdutos.Count > 0 && p.Ativo && p.Loja.Id == loja);
        }

        [HttpGet("grupos")]
        public IEnumerable<dynamic> Grupo(int loja)
        {
            return _context.Grupos.Include(p => p.GrupoProdutos)
                .ThenInclude(p => p.Produto)
                .Where(p => p.GrupoProdutos.Count(p=> p.Produto.Ativo) > 0 && p.Loja.Id == loja && p.Ativo)
                .Select(p => new
                {
                    Id = p.Id
                ,
                    Nome = p.Nome
                ,
                    Ordem = p.Ordem
                ,
                    Products = p.GrupoProdutos.Where(p=> p.Produto.Ativo).Select(q => q.Produto)
                });
        }

        [HttpGet("produtosgrupo")]
        public IEnumerable<Produto> ProdutosGrupo(int loja, int grupo)
        {
            return _context.Produtos.Include(p => p.GrupoProdutos)
                .ThenInclude(p => p.Grupo)
                .Where(p => p.GrupoProdutos.Where(p=> p.Produto.Ativo)
                                           .Count(z=> z.GrupoId == grupo) > 0 && p.Loja.Id == loja);
        }

        [HttpGet("imagens")]
        public IEnumerable<ProdutoFoto> Imagens(int grupo)
        {
            var list = _context.ProdutoFotos.Include(p=> p.Produto)
                                            .Where(p => p.Produto.GrupoProdutos.Where(p=> p.Produto.Ativo).First().GrupoId == grupo).ToList();
           
            return list;
        }

        [HttpGet("paginar")]
        public async Task<Paginar<Produto>> Paginar(int loja, int grupo, int indice, int tamanho)
        {
            if(grupo == -1)
                return await Paginar<Produto>.CreateAsync(_context.Produtos.Where(p => p.Loja.Id == loja && p.Ativo), indice, tamanho);
            else
            return await Paginar<Produto>.CreateAsync(_context.Produtos.Where(p=> p.Ativo && p.Loja.Id == loja && p.GrupoProdutos.Count(p=> p.GrupoId == grupo )> 0),  indice, tamanho);
        }

        [HttpGet("buscarporloja")]
        public async Task<Paginar<Produto>> BuscarPorLoja(int loja, string nomeproduto, int indice, int tamanho)
        {
            if (!string.IsNullOrEmpty(nomeproduto))
            {
                return await Paginar<Produto>.CreateAsync(_context.Produtos.Where(p => p.Ativo && p.Loja.Id == loja &&
                (p.Nome.ToUpper().Contains(nomeproduto.ToUpper()) || p.Descricao.ToUpper().Contains(nomeproduto.ToUpper()))).Include(p=> p.ProdutoFotos), indice, tamanho);
            }
            else {
                return await Paginar<Produto>.CreateAsync(_context.Produtos.Where(p => p.Ativo && p.Loja.Id == loja).Include(p=> p.ProdutoFotos), indice, tamanho);
            }
        }

        [HttpGet("buscar")]
        public async Task<Paginar<Produto>> Buscar(string nomeproduto, int indice, int tamanho)
        {
            return await Paginar<Produto>.CreateAsync(_context.Produtos.Where(p => p.Ativo && p.Nome.ToUpper().Contains(nomeproduto.ToUpper()) ||
             p.Descricao.ToUpper().Contains(nomeproduto.ToUpper()))
                .Include(p => p.Loja).Include(p => p.ProdutoFotos).OrderBy(p => p.Preco), indice, tamanho);
        }

        [HttpGet("imagensgrupo")]
        public IActionResult ImagensGrupo(int grupo)
        {
            var list = _context.GrupoProdutos
                                .Include(p => p.Produto)
                                .ThenInclude(p=> p.ProdutoFotos)
                                .Where(p => p.Produto.Ativo && p.GrupoId == grupo)
                                .SelectMany(p=> p.Produto.ProdutoFotos).ToList();
            //
            return Ok(list);
        }
    }
}
