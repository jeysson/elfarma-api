using System;
using System.Net;
using System.Net.Mail;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AllDelivery.Lib;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NetTopologySuite.Geometries;

namespace AllDelivery.Api.Controllers
{
    [ApiController]
    [Authorize("Bearer")]
    [Route("api/[controller]")]
    public class FormaPagamentoController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        readonly PasswordHasher _passwordHasher;

        public FormaPagamentoController(ApplicationDbContext context, IOptions<HashingOptions> options)
        {
            _context = context;
            _passwordHasher = new PasswordHasher(options);
        }

        [HttpGet("obterfp")]
        public async Task<IActionResult> ObterFP(uint fp) 
        {
            Mensageiro mensageiro = new Mensageiro(200, "Operação realizada com sucesso!");
            try 
            {
                var xx = _context.FormaPagamentos.FirstOrDefault(p => p.Id == fp);
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
        public async Task<IActionResult> Cadastrar(FormaPagamento fp) 
        {
            Mensageiro mensageiro = new Mensageiro(200 ,"Operação realizada com sucesso!");
            try
            {
                _context.Database.BeginTransaction();
                _context.FormaPagamentos.Add(fp);
                _context.SaveChanges();
                _context.Database.CommitTransaction();
            }
            catch (Exception ex)
            {
                mensageiro.Codigo = 300;
                mensageiro.Mensagem = ex.Message;
                _context.Database.RollbackTransaction();
            }
            return Ok(mensageiro);
        }

        [HttpDelete("excluir")]
        public async Task<IActionResult> Excluir(uint fp)
        {
            Mensageiro mensageiro = new Mensageiro(200 ,"Operação realizada com sucesso!");
            try
            {
                _context.Database.BeginTransaction();
                var cc = _context.FormaPagamentos.FirstOrDefault(p => p.Id == fp);
                if (cc != null)
                    _context.Entry<FormaPagamento>(cc).State = EntityState.Detached;
                _context.FormaPagamentos.Remove(cc);
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

        [HttpPut("atualizar")]
        public async Task<IActionResult> Atualizar(FormaPagamento fp) 
        {
            Mensageiro mensageiro = new Mensageiro(200, "Operação realizada com sucesso!");
            try
            {
                _context.Database.BeginTransaction();
                var cc = _context.FormaPagamentos.FirstOrDefault(p => p.Id == fp.Id);
                if (cc != null)
                    _context.Entry<FormaPagamento>(cc).State = EntityState.Detached;
                _context.FormaPagamentos.Update(fp);
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
        public async Task<IActionResult> Inativar(FormaPagamento fp)
        {
            Mensageiro mensageiro = new Mensageiro(200, string.Format("{0} com sucesso!", fp.Ativo ? "Ativada" : "Inativada"));
            try
            {
                _context.Database.BeginTransaction();
                _context.Attach(fp);
                _context.Entry<FormaPagamento>(fp).Property(p => p.Ativo).IsModified = true;
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

        [HttpGet("paging")]
        public async Task<IActionResult> Paging(int indice, int total) 
        {
            Mensageiro mensageiro = new Mensageiro(200, "Operação realizada com sucesso!");
            try
            {
                mensageiro.Dados = await Paginar<FormaPagamento>.CreateAsync(_context.FormaPagamentos.OrderBy(p => p.Id), indice, total);
                
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
