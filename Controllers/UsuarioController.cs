using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Security.Principal;
using System.Threading.Tasks;
using AllDelivery.Lib;
using FirebaseAdmin;
using FirebaseAdmin.Auth;
using FirebaseAdmin.Messaging;
using Google.Apis.Auth.OAuth2;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace AllDelivery.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UsuarioController : ControllerBase
    {
        private ApplicationDbContext _context;
        private readonly PasswordHasher _passwordHasher;
        private readonly SigningConfigurations _signingConfigurations;
        private readonly TokenConfiguration _tokenConfigurations;

        public UsuarioController(ApplicationDbContext context, IOptions<HashingOptions> options, SigningConfigurations signingConfigurations, TokenConfiguration tokenConfiguration) {
            _context = context;
            _passwordHasher = new PasswordHasher(options);
            _signingConfigurations = signingConfigurations;
            _tokenConfigurations = tokenConfiguration;
        }

        [AllowAnonymous]
        [HttpPost("autenticar")]
        public async Task<IActionResult> Autenticar(Login login)
        {
            Mensageiro mensageiro = new Mensageiro(200, "Operação realizada com sucesso!");
            try
            {
                _context.Database.BeginTransaction();

                await FirebaseMessaging.DefaultInstance.SendAsync(new Message { Token = login.TokenFCM }, true);

                var _usuario = _context.Usuarios.FirstOrDefault(p => p.Email == login.Email || p.TokenFCM == login.TokenFCM);
                //
                if (_usuario != null)
                {
                    _usuario.Anonimo = false;

                    if (string.IsNullOrEmpty(_usuario.Email))
                        _usuario.Email = login.Email;

                    if (_usuario.TokenFCM != login.TokenFCM)
                    {
                        _usuario.TokenFCM = login.TokenFCM;
                    }

                    /*  if (_passwordHasher.Check(_usuario.Senha, login.Senha))
                      {*/
                    ClaimsIdentity identity = new ClaimsIdentity(
                                               new GenericIdentity(_usuario.Id.ToString(), "Login"),
                                               new[] {
                                             new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString("N")),
                                             new Claim(JwtRegisteredClaimNames.UniqueName, _usuario.Id.ToString())
                                               }
                                           );
                    //
                    ClaimsPrincipal claimsPrincipal = new ClaimsPrincipal(identity);
                    HttpContext.User = claimsPrincipal;
                    //
                    if (!string.IsNullOrEmpty(login.Nome) && (_usuario.Nome == "Guest" || string.IsNullOrEmpty(_usuario.Nome)))
                    {
                        _usuario.Nome = login.Nome;

                    }

                    if (!string.IsNullOrEmpty(login.Sobrenome) && (_usuario.Sobrenome == " " || string.IsNullOrEmpty(_usuario.Sobrenome)))
                    {

                        _usuario.Sobrenome = login.Sobrenome;
                    }
                    //
                    _usuario.TokenCreate = DateTime.Now;
                    _usuario.TokenExpiration = _usuario.TokenCreate + TimeSpan.FromSeconds(_tokenConfigurations.Seconds);

                    var handler = new JwtSecurityTokenHandler();
                    var securityToken = handler.CreateToken(new SecurityTokenDescriptor
                    {
                        Issuer = _tokenConfigurations.Issuer,
                        Audience = _tokenConfigurations.Audience,
                        SigningCredentials = _signingConfigurations.SigningCredentials,
                        Subject = identity,
                        NotBefore = _usuario.TokenCreate,
                        Expires = _usuario.TokenExpiration
                    });
                    //Cria o token de acesso
                    _usuario.Token = handler.WriteToken(securityToken);
                    _usuario.DataUltimoLogin = DateTime.Now;
                    //salva o token de acesso
                    _context.Attach(_usuario);
                    _context.Entry<Usuario>(_usuario).Property(c => c.TokenCreate).IsModified = true;
                    _context.Entry<Usuario>(_usuario).Property(c => c.TokenExpiration).IsModified = true;
                    _context.Entry<Usuario>(_usuario).Property(c => c.Token).IsModified = true;
                    _context.Entry<Usuario>(_usuario).Property(c => c.DataUltimoLogin).IsModified = true;
                    _context.Entry<Usuario>(_usuario).Property(c => c.TokenFCM).IsModified = true;
                    _context.Entry<Usuario>(_usuario).Property(c => c.Anonimo).IsModified = true;
                    _context.Entry<Usuario>(_usuario).Property(c => c.Nome).IsModified = true;
                    _context.Entry<Usuario>(_usuario).Property(c => c.Sobrenome).IsModified = true;
                    _context.Entry<Usuario>(_usuario).Property(c => c.Email).IsModified = true;
                    //

                    _context.SaveChanges();
                    //
                    mensageiro.Dados = _usuario;
                }
                else
                {
                    mensageiro.Mensagem = "Usuário ou senha inválido!";
                }
                _context.Database.CommitTransaction();
                /*}
                else
                    mensageiro.Mensagem = "Usuário ou senha inválido!";   */
            }
            catch (Exception ex)
            {
                mensageiro.Codigo = 300;
                mensageiro.Mensagem = ex.Message;
                _context.Database.RollbackTransaction();
            }

            return Ok(mensageiro);
        }

        [AllowAnonymous]
        [HttpPost("autenticartoken")]
        public async Task<IActionResult> AutenticarToken(Login login)
        {
            Mensageiro mensageiro = new Mensageiro(200, "Operação realizada com sucesso!");
            try
            {
                _context.Database.BeginTransaction();
                await FirebaseMessaging.DefaultInstance.SendAsync(new Message { Token = login.TokenFCM }, true);
                //FirebaseToken fba = await FirebaseAuth.DefaultInstance.VerifyIdTokenAsync(login.TokenFCM);
                //if (fba. == TaskStatus.Faulted)
                //    throw new Exception("Token inválido!");

                var _usuario = _context.Usuarios.FirstOrDefault(p => p.TokenFCM == login.TokenFCM);

                if (_usuario == null)
                {
                    _usuario = new Usuario { TokenFCM = login.TokenFCM };
                    _usuario.CodeVerification = " ";
                    _usuario.Nome = "Guest";
                    _usuario.Sobrenome = " ";
                    _usuario.Anonimo = true;
                    _context.Usuarios.Add(_usuario);
                    _context.SaveChanges();
                }
                //
                ClaimsIdentity identity = new ClaimsIdentity(
                                               new GenericIdentity(_usuario.Id.ToString(), "Login"),
                                               new[] {
                                             new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString("N")),
                                             new Claim(JwtRegisteredClaimNames.UniqueName, _usuario.Id.ToString())
                                               }
                                           );
                //
                ClaimsPrincipal claimsPrincipal = new ClaimsPrincipal(identity);
                HttpContext.User = claimsPrincipal;
                //
                _usuario.TokenCreate = DateTime.Now;
                _usuario.TokenExpiration = _usuario.TokenCreate + TimeSpan.FromSeconds(_tokenConfigurations.Seconds);

                var handler = new JwtSecurityTokenHandler();
                var securityToken = handler.CreateToken(new SecurityTokenDescriptor
                {
                    Issuer = _tokenConfigurations.Issuer,
                    Audience = _tokenConfigurations.Audience,
                    SigningCredentials = _signingConfigurations.SigningCredentials,
                    Subject = identity,
                    NotBefore = _usuario.TokenCreate,
                    Expires = _usuario.TokenExpiration
                });
                //Cria o token de acesso
                _usuario.Token = handler.WriteToken(securityToken);
                _usuario.DataUltimoLogin = DateTime.Now;
                //salva o token de acesso
                _context.Attach(_usuario);
                _context.Entry<Usuario>(_usuario).Property(c => c.TokenCreate).IsModified = true;
                _context.Entry<Usuario>(_usuario).Property(c => c.TokenExpiration).IsModified = true;
                _context.Entry<Usuario>(_usuario).Property(c => c.Token).IsModified = true;
                _context.Entry<Usuario>(_usuario).Property(c => c.DataUltimoLogin).IsModified = true;
                _context.SaveChanges();
                //
                mensageiro.Dados = _usuario;
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


        [Authorize("Bearer")]
        [HttpPost("verificar")]
        public IActionResult Verificar(Login login)
        {
            Mensageiro mensageiro = new Mensageiro(200, "Operação realizada com sucesso!");
            try
            {
                _context.Database.BeginTransaction();

                var _usuario = _context.Usuarios.FirstOrDefault(p => p.Email == login.Email && p.CodeVerification == login.CodeVerification);

                if (_usuario != null)
                {
                    _usuario.Validated = true;
                    _context.Entry<Usuario>(_usuario).Property(p=> p.Validated).IsModified = true;
                    _context.SaveChanges();
                    _context.Database.CommitTransaction();
                    _usuario.Senha = null;
                    mensageiro.Dados = _usuario;

                }
                else
                    mensageiro.Mensagem = "Código inválido";
            }
            catch (Exception ex)
            {
                mensageiro.Mensagem = ex.Message;
                _context.Database.RollbackTransaction();
            }

            return Ok(mensageiro);
        }

        [Authorize("Bearer")]
        [HttpPost("novo")]
        public IActionResult Novo(Login login)
        {
            Mensageiro mensageiro = new Mensageiro(200, "Operação realizada com sucesso!");
            try
            {
                _context.Database.BeginTransaction();

                var _usuario = _context.Usuarios.FirstOrDefault(p => p.TokenFCM == login.TokenFCM);

                if (_usuario != null)
                {
                    _usuario.Validated = true;
                    _usuario.Email = login.Email;
                    _usuario.Nome = " ";
                    _usuario.Anonimo = false;
                    _context.Entry<Usuario>(_usuario).Property(p => p.Validated).IsModified = true;
                    _context.SaveChanges();
                    _context.Database.CommitTransaction();
                    _usuario.Senha = null;
                    mensageiro.Dados = _usuario;

                }
                else
                    mensageiro.Mensagem = "Código inválido";
            }
            catch (Exception ex)
            {
                mensageiro.Mensagem = ex.Message;
                _context.Database.RollbackTransaction();
            }

            return Ok(mensageiro);
        }
    }
}
