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
    public class EquipesController : Controller
    {
        private readonly ContextoWorkflow _contexto;

        public EquipesController(ContextoWorkflow contexto)
        {
            _contexto = contexto;
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

            var query = _contexto.Equipes
                .Include(e => e.EquipeUsuarios)
                    .ThenInclude(eu => eu.Usuario)
                .Where(e => e.Ativo)
                .AsQueryable();

            if (!ehAdminOuGestor)
            {
                // Líderes só visualizam a sua própria equipe
                query = query.Where(e => equipesLideradasIds.Contains(e.Id));
            }

            var equipes = await query
                .OrderBy(e => e.Nome)
                .ToListAsync();

            ViewBag.Usuarios = await _contexto.Usuarios.Where(u => u.Ativo).ToListAsync();

            return View(equipes);
        }

        [HttpPost]
        public async Task<IActionResult> Salvar(Guid? id, string nome, string descricao)
        {
            var (ehAdminOuGestor, equipesLideradasIds) = await ObterInfoAcessoUsuarioAsync();
            if (!ehAdminOuGestor && !equipesLideradasIds.Any())
            {
                TempData["Erro"] = "Acesso negado: Sem privilégios de gerenciamento.";
                return RedirectToAction("Index", "PainelGeral");
            }

            if (string.IsNullOrWhiteSpace(nome))
            {
                TempData["Erro"] = "O nome da equipe é obrigatório.";
                return RedirectToAction(nameof(Index));
            }

            try
            {
                if (id.HasValue && id.Value != Guid.Empty)
                {
                    // Editar - se não for admin, verifica se lidera a equipe sendo editada
                    if (!ehAdminOuGestor && !equipesLideradasIds.Contains(id.Value))
                    {
                        TempData["Erro"] = "Acesso negado: Você não possui permissão para editar esta equipe.";
                        return RedirectToAction(nameof(Index));
                    }

                    var equipe = await _contexto.Equipes.FindAsync(id.Value);
                    if (equipe == null)
                    {
                        TempData["Erro"] = "Equipe não localizada.";
                        return RedirectToAction(nameof(Index));
                    }

                    equipe.Nome = nome;
                    equipe.Descricao = descricao;
                    equipe.DataAtualizacao = DateTime.Now;

                    _contexto.Equipes.Update(equipe);
                    TempData["Sucesso"] = "Equipe atualizada com sucesso!";
                }
                else
                {
                    // Apenas Administrador/Gestor Global pode cadastrar novas equipes
                    if (!ehAdminOuGestor)
                    {
                        TempData["Erro"] = "Acesso negado: Apenas administradores globais podem criar novas equipes.";
                        return RedirectToAction(nameof(Index));
                    }

                    var novaEquipe = new Equipe
                    {
                        Nome = nome,
                        Descricao = descricao,
                        Ativo = true,
                        DataCriacao = DateTime.Now,
                        DataAtualizacao = DateTime.Now
                    };

                    await _contexto.Equipes.AddAsync(novaEquipe);
                    TempData["Sucesso"] = "Nova equipe criada com sucesso!";
                }

                await _contexto.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                TempData["Erro"] = $"Erro ao salvar equipe: {ex.Message}";
            }

            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        public async Task<IActionResult> VincularMembro(Guid equipeId, Guid usuarioId, bool chefe)
        {
            var (ehAdminOuGestor, equipesLideradasIds) = await ObterInfoAcessoUsuarioAsync();
            if (!ehAdminOuGestor && !equipesLideradasIds.Contains(equipeId))
            {
                TempData["Erro"] = "Acesso negado: Você não possui permissão para gerenciar os membros desta equipe.";
                return RedirectToAction("Index", "PainelGeral");
            }

            try
            {
                // Verifica se já está vinculado
                var jaVinculado = await _contexto.EquipeUsuarios
                    .AnyAsync(eu => eu.EquipeId == equipeId && eu.UsuarioId == usuarioId && eu.Ativo);

                if (jaVinculado)
                {
                    TempData["Erro"] = "Este colaborador já é membro desta equipe.";
                    return RedirectToAction(nameof(Index));
                }

                if (chefe)
                {
                    // Apenas admin/gestor global pode alterar/definir chefes de equipe
                    if (!ehAdminOuGestor)
                    {
                        TempData["Erro"] = "Acesso negado: Apenas administradores ou gestores globais podem nomear líderes de equipe.";
                        return RedirectToAction(nameof(Index));
                    }

                    // Remove chefia antiga desta equipe se houver
                    var chefeAntigo = await _contexto.EquipeUsuarios
                        .FirstOrDefaultAsync(eu => eu.EquipeId == equipeId && eu.AdministradorEquipe && eu.Ativo);
                    
                    if (chefeAntigo != null)
                    {
                        chefeAntigo.AdministradorEquipe = false;
                        _contexto.EquipeUsuarios.Update(chefeAntigo);
                    }
                }

                var vinculo = new EquipeUsuario
                {
                    EquipeId = equipeId,
                    UsuarioId = usuarioId,
                    AdministradorEquipe = chefe,
                    Ativo = true,
                    DataCriacao = DateTime.Now,
                    DataAtualizacao = DateTime.Now
                };

                await _contexto.EquipeUsuarios.AddAsync(vinculo);
                await _contexto.SaveChangesAsync();
                TempData["Sucesso"] = "Membro adicionado à equipe com sucesso!";
            }
            catch (Exception ex)
            {
                TempData["Erro"] = $"Erro ao vincular membro: {ex.Message}";
            }

            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        public async Task<IActionResult> RemoverMembro(Guid id)
        {
            var (ehAdminOuGestor, equipesLideradasIds) = await ObterInfoAcessoUsuarioAsync();

            try
            {
                var vinculo = await _contexto.EquipeUsuarios.FindAsync(id);
                if (vinculo == null)
                {
                    TempData["Erro"] = "Vínculo de membro não encontrado.";
                    return RedirectToAction(nameof(Index));
                }

                if (!ehAdminOuGestor && !equipesLideradasIds.Contains(vinculo.EquipeId))
                {
                    TempData["Erro"] = "Acesso negado: Você não possui permissão para gerenciar os membros desta equipe.";
                    return RedirectToAction("Index", "PainelGeral");
                }

                _contexto.EquipeUsuarios.Remove(vinculo);
                await _contexto.SaveChangesAsync();
                TempData["Sucesso"] = "Membro removido da equipe com sucesso.";
            }
            catch (Exception ex)
            {
                TempData["Erro"] = $"Erro ao remover membro: {ex.Message}";
            }

            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        public async Task<IActionResult> Excluir(Guid id)
        {
            var (ehAdminOuGestor, _) = await ObterInfoAcessoUsuarioAsync();
            if (!ehAdminOuGestor)
            {
                TempData["Erro"] = "Acesso negado: Apenas administradores ou gestores globais possuem permissão para excluir equipes.";
                return RedirectToAction("Index", "PainelGeral");
            }

            try
            {
                var equipe = await _contexto.Equipes.FindAsync(id);
                if (equipe == null)
                {
                    TempData["Erro"] = "Equipe não encontrada.";
                    return RedirectToAction(nameof(Index));
                }

                equipe.Ativo = false;
                _contexto.Equipes.Update(equipe);
                await _contexto.SaveChangesAsync();
                TempData["Sucesso"] = "Equipe removida com sucesso.";
            }
            catch (Exception ex)
            {
                TempData["Erro"] = $"Erro ao excluir equipe: {ex.Message}";
            }

            return RedirectToAction(nameof(Index));
        }
    }
}
