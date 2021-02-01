using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.EntityFrameworkCore;
using AllDelivery.Lib;
using Microsoft.EntityFrameworkCore.Internal;

namespace AllDelivery.Api
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext()
        {
        }
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }


        public DbSet<Loja> Lojas { get; set; }

        public DbSet<Produto> Produtos { get; set; }

        public DbSet<Grupo> Grupos { get; set; }

        public DbSet<GrupoProduto> GrupoProdutos { get; set; }

        public DbSet<ProdutoFoto> ProdutoFotos { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<GrupoProduto>()
                .HasKey(c => new { c.GrupoId, c.ProdutoId});

            modelBuilder.Entity<ProdutoFoto>()
                .HasKey(c => new { c.ProdutoId, c.Seq });
        }
    }
}
