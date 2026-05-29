using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SistemaWorkflow.Aplicacao.Interfaces;
using SistemaWorkflow.Dominio.Entidades;
using SistemaWorkflow.Dominio.Enums;
using SistemaWorkflow.Infraestrutura.BancoDados;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SistemaWorkflow.Blazor.Controllers
{
    [Microsoft.AspNetCore.Authorization.Authorize]
    public class TarefasController : Controller
    {
        private readonly IMotorWorkflow _motorWorkflow;
        private readonly ContextoWorkflow _contexto;

        public TarefasController(IMotorWorkflow motorWorkflow, ContextoWorkflow contexto)
        {
            _motorWorkflow = motorWorkflow;
            _contexto = contexto;
        }

        public async Task<IActionResult> Fila()
        {
            var emailLogado = User.Claims.FirstOrDefault(c => c.Type == System.Security.Claims.ClaimTypes.Email)?.Value;
            var usuario = await _contexto.Usuarios
                .Include(u => u.UsuarioPerfis).ThenInclude(up => up.Perfil)
                .Include(u => u.EquipeUsuarios)
                .FirstOrDefaultAsync(u => u.Email == emailLogado);

            if (usuario == null)
            {
                return RedirectToAction("Login", "Usuarios");
            }

            var equipesIds = usuario.EquipeUsuarios.Select(eu => eu.EquipeId).ToList();
            bool ehAdmin = usuario.UsuarioPerfis.Any(up => up.Perfil.Nome == "Administrador");

            // Filtragem avançada e real de tarefas com base na equipe e privilégios do usuário logado
            var query = _contexto.TarefasProcesso
                .Include(t => t.Atividade)
                    .ThenInclude(a => a.VersaoProcesso)
                        .ThenInclude(v => v.Processo)
                .Include(t => t.InstanciaProcesso)
                .Where(t => t.Status == StatusTarefaProcesso.Pendente && t.Ativo);

            if (!ehAdmin)
            {
                // Colaborador/Gestor comum só vê tarefas sem equipe definida ou destinadas a uma de suas equipes
                query = query.Where(t => t.EquipeResponsavelId == null || equipesIds.Contains(t.EquipeResponsavelId.Value));
            }

            var tarefas = await query
                .OrderByDescending(t => t.DataCriacao)
                .ToListAsync();

            // Processos homologados ativos para iniciar instâncias
            ViewBag.ProcessosAtivos = await _contexto.VersoesProcesso
                .Include(v => v.Processo)
                .Where(v => v.Status == StatusVersaoProcesso.Ativo && v.Ativo)
                .ToListAsync();

            return View(tarefas);
        }

        [HttpPost]
        public async Task<IActionResult> IniciarInstancia(Guid versaoId, string? titulo)
        {
            try
            {
                var emailLogado = User.Claims.FirstOrDefault(c => c.Type == System.Security.Claims.ClaimTypes.Email)?.Value;
                var usuario = await _contexto.Usuarios.FirstOrDefaultAsync(u => u.Email == emailLogado);
                if (usuario == null)
                {
                    TempData["Erro"] = "Usuário criador não localizado.";
                    return RedirectToAction(nameof(Fila));
                }

                string tituloInstancia = string.IsNullOrWhiteSpace(titulo) 
                     ? $"Instância iniciada em {DateTime.Now:dd/MM/yyyy HH:mm}" 
                     : titulo;

                var instancia = await _motorWorkflow.IniciarInstanciaAsync(versaoId, usuario.Id, tituloInstancia);
                TempData["Sucesso"] = $"Instância do processo '{instancia.VersaoProcesso.Processo.Nome}' iniciada com sucesso!";
            }
            catch (Exception ex)
            {
                TempData["Erro"] = $"Erro ao iniciar instância: {ex.Message}";
            }

            return RedirectToAction(nameof(Fila));
        }

        public async Task<IActionResult> Executar(Guid id)
        {
            var tarefa = await _contexto.TarefasProcesso
                .Include(t => t.InstanciaProcesso)
                .Include(t => t.Atividade)
                    .ThenInclude(a => a.Formulario)
                        .ThenInclude(f => f.Campos)
                            .ThenInclude(c => c.Opcoes)
                .Include(t => t.Atividade)
                    .ThenInclude(a => a.VersaoProcesso)
                        .ThenInclude(v => v.Processo)
                .FirstOrDefaultAsync(t => t.Id == id && t.Ativo);

            if (tarefa == null)
            {
                TempData["Erro"] = "Tarefa operacional não encontrada.";
                return RedirectToAction(nameof(Fila));
            }

            if (tarefa.Status != StatusTarefaProcesso.Pendente)
            {
                TempData["Erro"] = "Esta tarefa já foi concluída ou cancelada.";
                return RedirectToAction(nameof(Fila));
            }

            // Busca as transições possíveis para a etapa atual
            var transicoes = await _contexto.TransicoesAtividade
                .Where(t => t.AtividadeOrigemId == tarefa.AtividadeId && t.Ativo)
                .ToListAsync();

            ViewBag.Transicoes = transicoes;

            return View(tarefa);
        }

        [HttpPost]
        public async Task<IActionResult> Submeter(Guid id, string acaoSelecionada, IFormCollection form)
        {
            var tarefa = await _contexto.TarefasProcesso
                .Include(t => t.Atividade)
                    .ThenInclude(a => a.Formulario)
                        .ThenInclude(f => f.Campos)
                .FirstOrDefaultAsync(t => t.Id == id);

            if (tarefa == null)
            {
                TempData["Erro"] = "Tarefa operacional não encontrada.";
                return RedirectToAction(nameof(Fila));
            }

            try
            {
                var emailLogado = User.Claims.FirstOrDefault(c => c.Type == System.Security.Claims.ClaimTypes.Email)?.Value;
                var admin = await _contexto.Usuarios.FirstOrDefaultAsync(u => u.Email == emailLogado);
                if (admin == null)
                {
                    TempData["Erro"] = "Usuário executor não autenticado.";
                    return RedirectToAction(nameof(Fila));
                }

                // Processamento de dados dinâmicos do formulário
                var valoresCampos = new Dictionary<Guid, string>();
                if (tarefa.Atividade.Formulario != null)
                {
                    foreach (var campo in tarefa.Atividade.Formulario.Campos)
                    {
                        string chaveForm = $"campo_{campo.Id}";
                        string valorForm = form[chaveForm].ToString();

                        if (campo.Obrigatorio && string.IsNullOrWhiteSpace(valorForm))
                        {
                            TempData["Erro"] = $"O campo '{campo.Rotulo}' é obrigatório.";
                            return RedirectToAction(nameof(Executar), new { id });
                        }

                        valoresCampos[campo.Id] = valorForm ?? "";
                    }
                }

                await _motorWorkflow.CompletarTarefaAsync(id, admin.Id, valoresCampos, acaoSelecionada);
                TempData["Sucesso"] = "Etapa concluída e processo encaminhado com sucesso!";
            }
            catch (Exception ex)
            {
                TempData["Erro"] = $"Erro ao concluir etapa: {ex.Message}";
                return RedirectToAction(nameof(Executar), new { id });
            }

            return RedirectToAction(nameof(Fila));
        }
    }
}
