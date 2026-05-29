using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SistemaWorkflow.Dominio.Entidades;
using SistemaWorkflow.Infraestrutura.BancoDados;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace SistemaWorkflow.Blazor.Controllers
{
    [Microsoft.AspNetCore.Authorization.Authorize]
    public class PermissoesController : Controller
    {
        private readonly ContextoWorkflow _contexto;

        public PermissoesController(ContextoWorkflow contexto)
        {
            _contexto = contexto;
        }

        public async Task<IActionResult> Index()
        {
            if (!await ValidarAdminOuGestorAsync())
            {
                TempData["Erro"] = "Acesso negado: Apenas administradores ou gestores globais possuem permissão nesta tela.";
                return RedirectToAction("Index", "PainelGeral");
            }

            var perfis = await _contexto.Perfis.Where(p => p.Ativo).ToListAsync();
            var permissoes = await _contexto.Permissoes.Where(p => p.Ativo).ToListAsync();
            var perfilPermissoes = await _contexto.PerfilPermissoes.Where(pp => pp.Ativo).ToListAsync();

            ViewBag.Perfis = perfis;
            ViewBag.Permissoes = permissoes;
            ViewBag.PerfilPermissoes = perfilPermissoes;

            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Alternar(Guid perfilId, Guid permissaoId, bool associar)
        {
            if (!await ValidarAdminOuGestorAsync())
            {
                return Json(new { sucesso = false, erro = "Acesso negado: Apenas administradores ou gestores globais possuem permissão." });
            }

            try
            {
                var pp = await _contexto.PerfilPermissoes
                    .FirstOrDefaultAsync(x => x.PerfilId == perfilId && x.PermissaoId == permissaoId);

                if (associar)
                {
                    if (pp == null)
                    {
                        var novo = new PerfilPermissao
                        {
                            PerfilId = perfilId,
                            PermissaoId = permissaoId,
                            Ativo = true,
                            DataCriacao = DateTime.Now,
                            DataAtualizacao = DateTime.Now
                        };
                        await _contexto.PerfilPermissoes.AddAsync(novo);
                    }
                    else if (!pp.Ativo)
                    {
                        pp.Ativo = true;
                        pp.DataAtualizacao = DateTime.Now;
                        _contexto.PerfilPermissoes.Update(pp);
                    }
                }
                else
                {
                    if (pp != null)
                    {
                        _contexto.PerfilPermissoes.Remove(pp);
                    }
                }

                await _contexto.SaveChangesAsync();
                return Json(new { sucesso = true });
            }
            catch (Exception ex)
            {
                return Json(new { sucesso = false, erro = ex.Message });
            }
        }

        private async Task<bool> ValidarAdminOuGestorAsync()
        {
            var emailLogado = User.Claims.FirstOrDefault(c => c.Type == System.Security.Claims.ClaimTypes.Email)?.Value;
            var usuario = await _contexto.Usuarios
                .Include(u => u.UsuarioPerfis).ThenInclude(up => up.Perfil)
                .FirstOrDefaultAsync(u => u.Email == emailLogado);

            return usuario?.UsuarioPerfis.Any(up => up.Perfil.Nome == "Administrador" || up.Perfil.Nome == "Gestor") == true;
        }
    }
}
