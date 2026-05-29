using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SistemaWorkflow.Aplicacao.Interfaces;
using SistemaWorkflow.Dominio.Entidades;
using SistemaWorkflow.Dominio.Enums;
using SistemaWorkflow.Infraestrutura.BancoDados;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace SistemaWorkflow.Blazor.Controllers
{
    [Microsoft.AspNetCore.Authorization.Authorize]
    public class ProcessosController : Controller
    {
        private readonly IServicoProcesso _servicoProcesso;
        private readonly ContextoWorkflow _contexto;

        public ProcessosController(IServicoProcesso servicoProcesso, ContextoWorkflow contexto)
        {
            _servicoProcesso = servicoProcesso;
            _contexto = contexto;
        }

        public async Task<IActionResult> Index()
        {
            if (!await ValidarLiderDeQualquerEquipeAsync())
            {
                TempData["Erro"] = "Acesso negado: Apenas gestores, administradores ou líderes de equipe possuem permissão de gerenciamento de processos.";
                return RedirectToAction("Index", "PainelGeral");
            }

            var emailLogado = User.Claims.FirstOrDefault(c => c.Type == System.Security.Claims.ClaimTypes.Email)?.Value;
            var usuario = await _contexto.Usuarios
                .Include(u => u.UsuarioPerfis).ThenInclude(up => up.Perfil)
                .Include(u => u.EquipeUsuarios)
                .FirstOrDefaultAsync(u => u.Email == emailLogado);

            bool ehAdminOuGestor = usuario?.UsuarioPerfis.Any(up => up.Perfil.Nome == "Administrador" || up.Perfil.Nome == "Gestor") == true;
            
            var equipesLideradasIds = usuario?.EquipeUsuarios
                .Where(eu => eu.AdministradorEquipe && eu.Ativo)
                .Select(eu => eu.EquipeId)
                .ToList() ?? new List<Guid>();

            var queryProcessos = _contexto.Processos
                .Include(p => p.Equipe)
                .Include(p => p.Versoes)
                    .ThenInclude(v => v.Atividades)
                .Where(p => p.Ativo);

            if (!ehAdminOuGestor)
            {
                // Líderes vêem os processos de suas equipes E processos de outras equipes onde sua equipe é responsável por alguma etapa/atividade
                queryProcessos = queryProcessos.Where(p => 
                    equipesLideradasIds.Contains(p.EquipeId) || 
                    p.Versoes.Any(v => v.Ativo && v.Atividades.Any(a => a.Ativo && a.ResponsavelEquipeId.HasValue && equipesLideradasIds.Contains(a.ResponsavelEquipeId.Value)))
                );
            }

            var processos = await queryProcessos
                .OrderBy(p => p.Nome)
                .ToListAsync();

            var queryEquipes = _contexto.Equipes.Where(e => e.Ativo);
            if (!ehAdminOuGestor)
            {
                queryEquipes = queryEquipes.Where(e => equipesLideradasIds.Contains(e.Id));
            }
            ViewBag.Equipes = await queryEquipes.ToListAsync();

            ViewBag.VersoesAtivasContagem = await _contexto.VersoesProcesso
                .CountAsync(v => v.Status == StatusVersaoProcesso.Ativo && v.Ativo);

            ViewBag.InstanciasAtivasContagem = await _contexto.InstanciasProcesso
                .CountAsync(i => i.Status == StatusInstanciaProcesso.Ativa && i.Ativo);

            var agrupadoInstancias = await _contexto.InstanciasProcesso
                .Include(i => i.VersaoProcesso)
                .Where(i => i.Ativo)
                .GroupBy(i => i.VersaoProcesso.ProcessoId)
                .Select(g => new { ProcessoId = g.Key, Qtd = g.Count() })
                .ToDictionaryAsync(a => a.ProcessoId, a => a.Qtd);

            ViewBag.InstanciasContagem = agrupadoInstancias;

            return View(processos);
        }

        [HttpPost]
        public async Task<IActionResult> CriarNovo(string nome, string descricao, Guid equipeId)
        {
            if (!await ValidarPermissaoEquipeAsync(equipeId))
            {
                TempData["Erro"] = "Acesso negado: Você não possui privilégios de gestor ou líder da equipe selecionada.";
                return RedirectToAction(nameof(Index));
            }

            if (string.IsNullOrWhiteSpace(nome))
            {
                TempData["Erro"] = "Insira o nome do processo.";
                return RedirectToAction(nameof(Index));
            }

            try
            {
                var proc = await _servicoProcesso.CriarProcessoAsync(nome, descricao, equipeId);
                var ver = await _servicoProcesso.CriarVersaoProcessoAsync(proc.Id);
                TempData["Sucesso"] = "Processo e versão rascunho criados! Desenhe o fluxo abaixo.";
                return RedirectToAction(nameof(Desenhar), new { versaoId = ver.Id });
            }
            catch (Exception ex)
            {
                TempData["Erro"] = $"Erro ao criar processo: {ex.Message}";
                return RedirectToAction(nameof(Index));
            }
        }

        public async Task<IActionResult> Desenhar(Guid versaoId)
        {
            if (!await ValidarPermissaoProcessoAsync(versaoId))
            {
                TempData["Erro"] = "Acesso negado: Você não possui privilégios de gestor ou líder da equipe responsável por este processo.";
                return RedirectToAction(nameof(Index));
            }

            var versao = await _servicoProcesso.ObterVersaoCompletaAsync(versaoId);
            if (versao == null)
            {
                TempData["Erro"] = "Versão do processo não encontrada.";
                return RedirectToAction(nameof(Index));
            }

            ViewBag.Equipes = await _contexto.Equipes.Where(e => e.Ativo).ToListAsync();
            ViewBag.Formularios = await _contexto.Formularios.Where(f => f.Ativo).ToListAsync();
            
            ViewBag.Transicoes = await _contexto.TransicoesAtividade
                .Where(t => t.VersaoProcessoId == versaoId && t.Ativo)
                .ToListAsync();

            return View(versao);
        }

        [HttpPost]
        public async Task<IActionResult> AdicionarAtividade(
            Guid versaoProcessoId,
            string nome,
            string descricao,
            int SlaMinutos,
            Guid? responsavelEquipeId,
            Guid? formularioId,
            bool ehInicial,
            bool ehFinal)
        {
            if (!await ValidarPermissaoProcessoAsync(versaoProcessoId))
            {
                TempData["Erro"] = "Acesso negado: Você não possui privilégios de gestor ou líder da equipe responsável por este processo.";
                return RedirectToAction(nameof(Index));
            }

            if (string.IsNullOrWhiteSpace(nome))
            {
                TempData["Erro"] = "O nome da etapa é obrigatório.";
                return RedirectToAction(nameof(Desenhar), new { versaoId = versaoProcessoId });
            }

            try
            {
                await _servicoProcesso.AdicionarAtividadeAsync(
                    versaoProcessoId,
                    nome,
                    descricao,
                    SlaMinutos,
                    null,
                    responsavelEquipeId,
                    formularioId,
                    ehInicial,
                    ehFinal
                );
                TempData["Sucesso"] = "Etapa adicionada com sucesso!";
            }
            catch (Exception ex)
            {
                TempData["Erro"] = $"Erro ao adicionar etapa: {ex.Message}";
            }

            return RedirectToAction(nameof(Desenhar), new { versaoId = versaoProcessoId });
        }

        [HttpPost]
        public async Task<IActionResult> AdicionarTransicao(
            Guid versaoProcessoId,
            Guid origemId,
            Guid destinoId,
            string nomeAcao,
            string? condicao)
        {
            if (!await ValidarPermissaoProcessoAsync(versaoProcessoId))
            {
                TempData["Erro"] = "Acesso negado: Você não possui privilégios de gestor ou líder da equipe responsável por este processo.";
                return RedirectToAction(nameof(Index));
            }

            if (string.IsNullOrWhiteSpace(nomeAcao))
            {
                TempData["Erro"] = "O nome da ação da transição é obrigatório.";
                return RedirectToAction(nameof(Desenhar), new { versaoId = versaoProcessoId });
            }

            if (origemId == destinoId)
            {
                TempData["Erro"] = "A etapa de origem deve ser diferente da etapa de destino.";
                return RedirectToAction(nameof(Desenhar), new { versaoId = versaoProcessoId });
            }

            try
            {
                await _servicoProcesso.AdicionarTransicaoAsync(
                    versaoProcessoId,
                    origemId,
                    destinoId,
                    condicao ?? "",
                    nomeAcao
                );
                TempData["Sucesso"] = "Transição conectada com sucesso!";
            }
            catch (Exception ex)
            {
                TempData["Erro"] = $"Erro ao conectar transição: {ex.Message}";
            }

            return RedirectToAction(nameof(Desenhar), new { versaoId = versaoProcessoId });
        }

        [HttpPost]
        public async Task<IActionResult> CriarNovaVersao(Guid processoId)
        {
            var processo = await _contexto.Processos.FindAsync(processoId);
            if (processo == null)
            {
                TempData["Erro"] = "Processo não encontrado.";
                return RedirectToAction(nameof(Index));
            }

            if (!await ValidarPermissaoEquipeAsync(processo.EquipeId))
            {
                TempData["Erro"] = "Acesso negado: Você não possui privilégios de gestor ou líder da equipe proprietária deste processo.";
                return RedirectToAction(nameof(Index));
            }

            try
            {
                var ver = await _servicoProcesso.CriarVersaoProcessoAsync(processoId);
                TempData["Sucesso"] = "Nova versão rascunho criada! Comece a desenhá-la.";
                return RedirectToAction(nameof(Desenhar), new { versaoId = ver.Id });
            }
            catch (Exception ex)
            {
                TempData["Erro"] = $"Erro ao criar versão: {ex.Message}";
                return RedirectToAction(nameof(Index));
            }
        }

        public async Task<IActionResult> Homologacoes()
        {
            if (!await ValidarAdminOuGestorAsync())
            {
                TempData["Erro"] = "Acesso negado: Apenas administradores ou gestores globais possuem permissão para homologar processos.";
                return RedirectToAction("Index", "PainelGeral");
            }

            var rascunhos = await _contexto.VersoesProcesso
                .Include(v => v.Processo)
                    .ThenInclude(p => p.Equipe)
                .Where(v => v.Status == StatusVersaoProcesso.Rascunho && v.Ativo)
                .OrderBy(v => v.DataCriacao)
                .ToListAsync();

            return View(rascunhos);
        }

        public async Task<IActionResult> Revisar(Guid id)
        {
            if (!await ValidarAdminOuGestorAsync())
            {
                TempData["Erro"] = "Acesso negado: Apenas administradores ou gestores globais possuem permissão para revisar rascunhos.";
                return RedirectToAction("Index", "PainelGeral");
            }

            var versao = await _servicoProcesso.ObterVersaoCompletaAsync(id);
            if (versao == null)
            {
                TempData["Erro"] = "Rascunho não encontrado.";
                return RedirectToAction(nameof(Homologacoes));
            }

            return View(versao);
        }

        [HttpPost]
        public async Task<IActionResult> Homologar(Guid versaoId, bool aprovado, string justificativa)
        {
            if (!await ValidarAdminOuGestorAsync())
            {
                TempData["Erro"] = "Acesso negado: Apenas administradores ou gestores globais possuem permissão para homologar processos.";
                return RedirectToAction("Index", "PainelGeral");
            }

            if (string.IsNullOrWhiteSpace(justificativa))
            {
                TempData["Erro"] = "Insira uma justificativa para registrar a homologação.";
                return RedirectToAction(nameof(Revisar), new { id = versaoId });
            }

            try
            {
                var emailLogado = User.Claims.FirstOrDefault(c => c.Type == System.Security.Claims.ClaimTypes.Email)?.Value ?? "admin@workflow.com.br";
                var admin = await _contexto.Usuarios.FirstOrDefaultAsync(u => u.Email == emailLogado);
                if (admin == null)
                {
                    TempData["Erro"] = "Usuário homologador não localizado.";
                    return RedirectToAction(nameof(Homologacoes));
                }

                await _servicoProcesso.HomologarVersaoAsync(versaoId, admin.Id, aprovado, justificativa);
                TempData["Sucesso"] = aprovado 
                    ? "Processo homologado e publicado em produção com sucesso!" 
                    : "Rascunho rejeitado e devolvido para edição.";
            }
            catch (Exception ex)
            {
                TempData["Erro"] = $"Erro ao homologar processo: {ex.Message}";
            }

            return RedirectToAction(nameof(Homologacoes));
        }

        private async Task<bool> ValidarAdminOuGestorAsync()
        {
            var emailLogado = User.Claims.FirstOrDefault(c => c.Type == System.Security.Claims.ClaimTypes.Email)?.Value;
            var usuario = await _contexto.Usuarios
                .Include(u => u.UsuarioPerfis).ThenInclude(up => up.Perfil)
                .FirstOrDefaultAsync(u => u.Email == emailLogado);

            return usuario?.UsuarioPerfis.Any(up => up.Perfil.Nome == "Administrador" || up.Perfil.Nome == "Gestor") == true;
        }

        private async Task<bool> ValidarLiderDeQualquerEquipeAsync()
        {
            var emailLogado = User.Claims.FirstOrDefault(c => c.Type == System.Security.Claims.ClaimTypes.Email)?.Value;
            var usuario = await _contexto.Usuarios
                .Include(u => u.UsuarioPerfis).ThenInclude(up => up.Perfil)
                .Include(u => u.EquipeUsuarios)
                .FirstOrDefaultAsync(u => u.Email == emailLogado);

            if (usuario == null) return false;

            bool ehAdminOuGestor = usuario.UsuarioPerfis.Any(up => up.Perfil.Nome == "Administrador" || up.Perfil.Nome == "Gestor");
            bool ehLider = usuario.EquipeUsuarios.Any(eu => eu.AdministradorEquipe && eu.Ativo);

            return ehAdminOuGestor || ehLider;
        }

        private async Task<bool> ValidarPermissaoEquipeAsync(Guid equipeId)
        {
            var emailLogado = User.Claims.FirstOrDefault(c => c.Type == System.Security.Claims.ClaimTypes.Email)?.Value;
            var usuario = await _contexto.Usuarios
                .Include(u => u.UsuarioPerfis).ThenInclude(up => up.Perfil)
                .Include(u => u.EquipeUsuarios)
                .FirstOrDefaultAsync(u => u.Email == emailLogado);

            if (usuario == null) return false;

            bool ehAdminOuGestor = usuario.UsuarioPerfis.Any(up => up.Perfil.Nome == "Administrador" || up.Perfil.Nome == "Gestor");
            bool ehLiderDestaEquipe = usuario.EquipeUsuarios.Any(eu => eu.EquipeId == equipeId && eu.AdministradorEquipe && eu.Ativo);

            return ehAdminOuGestor || ehLiderDestaEquipe;
        }

        private async Task<bool> ValidarPermissaoProcessoAsync(Guid versaoId)
        {
            var versao = await _contexto.VersoesProcesso
                .Include(v => v.Processo)
                .Include(v => v.Atividades)
                .FirstOrDefaultAsync(v => v.Id == versaoId);

            if (versao == null) return false;

            var emailLogado = User.Claims.FirstOrDefault(c => c.Type == System.Security.Claims.ClaimTypes.Email)?.Value;
            var usuario = await _contexto.Usuarios
                .Include(u => u.UsuarioPerfis).ThenInclude(up => up.Perfil)
                .Include(u => u.EquipeUsuarios)
                .FirstOrDefaultAsync(u => u.Email == emailLogado);

            if (usuario == null) return false;

            bool ehAdminOuGestor = usuario.UsuarioPerfis.Any(up => up.Perfil.Nome == "Administrador" || up.Perfil.Nome == "Gestor");
            if (ehAdminOuGestor) return true;

            var equipesLideradasIds = usuario.EquipeUsuarios
                .Where(eu => eu.AdministradorEquipe && eu.Ativo)
                .Select(eu => eu.EquipeId)
                .ToList();

            // Lidera a equipe dona do processo
            bool lideraDona = equipesLideradasIds.Contains(versao.Processo.EquipeId);
            if (lideraDona) return true;

            // Lidera qualquer equipe envolvida em qualquer atividade deste processo
            bool lideraEnvolvida = versao.Atividades.Any(a => a.Ativo && a.ResponsavelEquipeId.HasValue && equipesLideradasIds.Contains(a.ResponsavelEquipeId.Value));
            
            return lideraEnvolvida;
        }
    }
}
