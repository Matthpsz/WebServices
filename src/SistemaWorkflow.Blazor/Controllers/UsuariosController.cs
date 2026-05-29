using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SistemaWorkflow.Aplicacao.Interfaces;
using SistemaWorkflow.Dominio.Entidades;
using SistemaWorkflow.Infraestrutura.BancoDados;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;

namespace SistemaWorkflow.Blazor.Controllers
{
    [Authorize]
    public class UsuariosController : Controller
    {
        private readonly ContextoWorkflow _contexto;
        private readonly IServicoCriptografia _criptografia;

        public UsuariosController(ContextoWorkflow contexto, IServicoCriptografia criptografia)
        {
            _contexto = contexto;
            _criptografia = criptografia;
        }

        private async Task<(bool ehAdminOuGestor, List<Guid> equipesLideradasIds)> ObterInfoAcessoUsuarioAsync()
        {
            var emailLogado = User.Claims.FirstOrDefault(c => c.Type == System.Security.Claims.ClaimTypes.Email)?.Value;
            var usuario = await _contexto.Usuarios
                .Include(u => u.UsuarioPerfis).ThenInclude(up => up.Perfil)
                .Include(u => u.EquipeUsuarios)
                .FirstOrDefaultAsync(u => u.Email == emailLogado);

            if (usuario == null) return (false, new List<Guid>());

            bool ehAdminOuGestor = usuario.UsuarioPerfis.Any(up => up.Perfil.Nome == "Administrador" || up.Perfil.Nome == "Gestor");
            
            var equipesLideradasIds = usuario.EquipeUsuarios
                .Where(eu => eu.AdministradorEquipe && eu.Ativo)
                .Select(eu => eu.EquipeId)
                .ToList();

            return (ehAdminOuGestor, equipesLideradasIds);
        }

        public async Task<IActionResult> Index()
        {
            var (ehAdminOuGestor, equipesLideradasIds) = await ObterInfoAcessoUsuarioAsync();
            
            if (!ehAdminOuGestor && !equipesLideradasIds.Any())
            {
                TempData["Erro"] = "Acesso negado: Apenas administradores, gestores ou líderes de equipe possuem permissão nesta tela.";
                return RedirectToAction("Index", "PainelGeral");
            }

            var query = _contexto.Usuarios
                .Include(u => u.UsuarioPerfis)
                    .ThenInclude(up => up.Perfil)
                .Include(u => u.EquipeUsuarios)
                .AsQueryable();

            if (!ehAdminOuGestor)
            {
                // Líderes só vêem colaboradores das equipes que lideram
                query = query.Where(u => u.EquipeUsuarios.Any(eu => equipesLideradasIds.Contains(eu.EquipeId) && eu.Ativo));
            }

            var usuarios = await query
                .OrderBy(u => u.Nome)
                .ToListAsync();

            ViewBag.Perfis = await _contexto.Perfis.Where(p => p.Ativo).ToListAsync();

            return View(usuarios);
        }

        [HttpPost]
        public async Task<IActionResult> Salvar(Guid? id, string nome, string email, string? senha, Guid perfilId, bool ativo)
        {
            var (ehAdminOuGestor, equipesLideradasIds) = await ObterInfoAcessoUsuarioAsync();
            if (!ehAdminOuGestor && !equipesLideradasIds.Any())
            {
                TempData["Erro"] = "Acesso negado: Apenas administradores, gestores ou líderes de equipe possuem permissão para esta ação.";
                return RedirectToAction("Index", "PainelGeral");
            }

            if (string.IsNullOrWhiteSpace(nome) || string.IsNullOrWhiteSpace(email))
            {
                TempData["Erro"] = "Nome e e-mail são obrigatórios.";
                return RedirectToAction(nameof(Index));
            }

            try
            {
                if (id.HasValue && id.Value != Guid.Empty)
                {
                    // Editar - se não for admin, verifica se o usuário pertence a uma das equipes lideradas pelo editor
                    if (!ehAdminOuGestor)
                    {
                        bool pertenceEquipeLiderada = await _contexto.EquipeUsuarios
                            .AnyAsync(eu => eu.UsuarioId == id.Value && equipesLideradasIds.Contains(eu.EquipeId) && eu.Ativo);

                        if (!pertenceEquipeLiderada)
                        {
                            TempData["Erro"] = "Acesso negado: Este colaborador não pertence a nenhuma das equipes sob sua liderança.";
                            return RedirectToAction(nameof(Index));
                        }
                    }

                    var usuario = await _contexto.Usuarios
                        .Include(u => u.UsuarioPerfis)
                        .FirstOrDefaultAsync(u => u.Id == id.Value);

                    if (usuario == null)
                    {
                        TempData["Erro"] = "Usuário não localizado.";
                        return RedirectToAction(nameof(Index));
                    }

                    usuario.Nome = nome;
                    usuario.Email = email;
                    usuario.Ativo = ativo;
                    usuario.DataAtualizacao = DateTime.Now;

                    if (!string.IsNullOrWhiteSpace(senha))
                    {
                        usuario.SenhaHash = _criptografia.CriptografarSenha(senha);
                    }

                    // Atualiza Perfil: remove o antigo e adiciona o novo separadamente
                    // para evitar conflito de concorrência otimista ao chamar Usuarios.Update()
                    var perfisAtuais = usuario.UsuarioPerfis.ToList();
                    foreach (var p in perfisAtuais)
                    {
                        _contexto.UsuarioPerfis.Remove(p);
                    }
                    await _contexto.SaveChangesAsync();

                    await _contexto.UsuarioPerfis.AddAsync(new UsuarioPerfil
                    {
                        PerfilId = perfilId,
                        UsuarioId = usuario.Id,
                        Ativo = true,
                        DataCriacao = DateTime.Now,
                        DataAtualizacao = DateTime.Now
                    });

                    // Não chamar Usuarios.Update() — a entidade já é rastreada pelo contexto
                    TempData["Sucesso"] = "Dados do colaborador atualizados com sucesso!";
                }
                else
                {
                    // Cadastrar
                    if (string.IsNullOrWhiteSpace(senha))
                    {
                        TempData["Erro"] = "Uma senha inicial é obrigatória para novos colaboradores.";
                        return RedirectToAction(nameof(Index));
                    }

                    // Verifica duplicidade de e-mail
                    if (await _contexto.Usuarios.AnyAsync(u => u.Email == email))
                    {
                        TempData["Erro"] = "Este e-mail já está cadastrado no sistema.";
                        return RedirectToAction(nameof(Index));
                    }

                    var novoUsuario = new Usuario
                    {
                        Nome = nome,
                        Email = email,
                        SenhaHash = _criptografia.CriptografarSenha(senha),
                        Ativo = ativo,
                        DataCriacao = DateTime.Now,
                        DataAtualizacao = DateTime.Now
                    };

                    novoUsuario.UsuarioPerfis.Add(new UsuarioPerfil
                    {
                        PerfilId = perfilId,
                        UsuarioId = novoUsuario.Id,
                        Ativo = true,
                        DataCriacao = DateTime.Now,
                        DataAtualizacao = DateTime.Now
                    });

                    await _contexto.Usuarios.AddAsync(novoUsuario);

                    // Se for novo cadastro e o criador for líder de equipe (não-admin), vincula automaticamente à primeira equipe liderada
                    if (!ehAdminOuGestor && equipesLideradasIds.Any())
                    {
                        var vinculo = new EquipeUsuario
                        {
                            EquipeId = equipesLideradasIds.First(),
                            UsuarioId = novoUsuario.Id,
                            AdministradorEquipe = false,
                            Ativo = true,
                            DataCriacao = DateTime.Now,
                            DataAtualizacao = DateTime.Now
                        };
                        await _contexto.EquipeUsuarios.AddAsync(vinculo);
                    }

                    TempData["Sucesso"] = "Novo colaborador cadastrado com sucesso!";
                }

                await _contexto.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                TempData["Erro"] = $"Erro ao salvar colaborador: {ex.Message}";
            }

            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        public async Task<IActionResult> Excluir(Guid id)
        {
            var (ehAdminOuGestor, equipesLideradasIds) = await ObterInfoAcessoUsuarioAsync();
            if (!ehAdminOuGestor && !equipesLideradasIds.Any())
            {
                TempData["Erro"] = "Acesso negado: Apenas administradores, gestores ou líderes de equipe possuem permissão para esta ação.";
                return RedirectToAction("Index", "PainelGeral");
            }

            if (!ehAdminOuGestor)
            {
                bool pertenceEquipeLiderada = await _contexto.EquipeUsuarios
                    .AnyAsync(eu => eu.UsuarioId == id && equipesLideradasIds.Contains(eu.EquipeId) && eu.Ativo);

                if (!pertenceEquipeLiderada)
                {
                    TempData["Erro"] = "Acesso negado: Este colaborador não pertence a nenhuma das equipes sob sua liderança.";
                    return RedirectToAction(nameof(Index));
                }
            }

            try
            {
                var usuario = await _contexto.Usuarios.FindAsync(id);
                if (usuario == null)
                {
                    TempData["Erro"] = "Usuário não localizado.";
                    return RedirectToAction(nameof(Index));
                }

                if (usuario.Email == "admin@workflow.com.br")
                {
                    TempData["Erro"] = "Não é permitido excluir o administrador padrão do sistema.";
                    return RedirectToAction(nameof(Index));
                }

                usuario.Ativo = false;
                _contexto.Usuarios.Update(usuario);
                await _contexto.SaveChangesAsync();
                TempData["Sucesso"] = "Colaborador desativado do sistema com sucesso.";
            }
            catch (Exception ex)
            {
                TempData["Erro"] = $"Erro ao remover colaborador: {ex.Message}";
            }

            return RedirectToAction(nameof(Index));
        }

        [AllowAnonymous]
        [HttpGet]
        public IActionResult Login()
        {
            if (User.Identity?.IsAuthenticated == true)
            {
                return RedirectToAction("Index", "PainelGeral");
            }
            return View();
        }

        [AllowAnonymous]
        [HttpPost]
        public async Task<IActionResult> Logar(string email, string senha)
        {
            if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(senha))
            {
                TempData["Erro"] = "E-mail e senha são obrigatórios.";
                return RedirectToAction(nameof(Login));
            }

            var usuario = await _contexto.Usuarios
                .Include(u => u.UsuarioPerfis)
                    .ThenInclude(up => up.Perfil)
                .FirstOrDefaultAsync(u => u.Email == email && u.Ativo);

            if (usuario == null || !_criptografia.VerificarSenha(senha, usuario.SenhaHash))
            {
                TempData["Erro"] = "Credenciais incorretas ou conta inativa.";
                return RedirectToAction(nameof(Login));
            }

            var perfil = usuario.UsuarioPerfis.FirstOrDefault()?.Perfil?.Nome ?? "Colaborador";

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, usuario.Nome),
                new Claim(ClaimTypes.Email, usuario.Email),
                new Claim(ClaimTypes.Role, perfil)
            };

            var identidade = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var principal = new ClaimsPrincipal(identidade);

            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal);

            TempData["Sucesso"] = $"Bem-vindo de volta, {usuario.Nome}!";
            return RedirectToAction("Index", "PainelGeral");
        }

        [HttpGet]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            TempData["Sucesso"] = "Sessão encerrada com sucesso.";
            return RedirectToAction(nameof(Login));
        }
    }
}
