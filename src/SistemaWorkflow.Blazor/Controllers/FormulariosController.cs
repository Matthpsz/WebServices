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
    public class FormulariosController : Controller
    {
        private readonly IServicoFormulario _servicoFormulario;
        private readonly ContextoWorkflow _contexto;

        public FormulariosController(IServicoFormulario servicoFormulario, ContextoWorkflow contexto)
        {
            _servicoFormulario = servicoFormulario;
            _contexto = contexto;
        }

        public async Task<IActionResult> Criar(Guid? id)
        {
            if (!await ValidarAdminOuGestorAsync())
            {
                TempData["Erro"] = "Acesso negado: Apenas administradores ou gestores globais possuem permissão nesta tela.";
                return RedirectToAction("Index", "PainelGeral");
            }

            ViewBag.Formularios = await _servicoFormulario.ObterTodosAsync();
            Formulario? formularioSelecionado = null;

            if (id.HasValue)
            {
                formularioSelecionado = await _contexto.Formularios
                    .Include(f => f.Campos)
                        .ThenInclude(c => c.Opcoes)
                    .FirstOrDefaultAsync(f => f.Id == id.Value && f.Ativo);
            }

            return View(formularioSelecionado);
        }

        [HttpPost]
        public async Task<IActionResult> CriarNovo(string nome, string descricao)
        {
            if (!await ValidarAdminOuGestorAsync())
            {
                TempData["Erro"] = "Acesso negado: Apenas administradores ou gestores globais possuem permissão para esta ação.";
                return RedirectToAction("Index", "PainelGeral");
            }

            if (string.IsNullOrWhiteSpace(nome))
            {
                TempData["Erro"] = "Por favor, informe o nome do formulário.";
                return RedirectToAction(nameof(Criar));
            }

            try
            {
                var form = await _servicoFormulario.CriarFormularioAsync(nome, descricao);
                TempData["Sucesso"] = "Formulário criado! Agora adicione alguns campos.";
                return RedirectToAction(nameof(Criar), new { id = form.Id });
            }
            catch (Exception ex)
            {
                TempData["Erro"] = $"Erro ao criar formulário: {ex.Message}";
                return RedirectToAction(nameof(Criar));
            }
        }

        [HttpPost]
        public async Task<IActionResult> AdicionarCampo(
            Guid formularioId,
            string label,
            TipoCampoFormulario tipo,
            bool obrigatorio,
            string? dicaAjuda,
            string? opcoesTexto)
        {
            if (!await ValidarAdminOuGestorAsync())
            {
                TempData["Erro"] = "Acesso negado: Apenas administradores ou gestores globais possuem permissão para esta ação.";
                return RedirectToAction("Index", "PainelGeral");
            }

            if (string.IsNullOrWhiteSpace(label))
            {
                TempData["Erro"] = "O rótulo/nome do campo é obrigatório.";
                return RedirectToAction(nameof(Criar), new { id = formularioId });
            }

            try
            {
                List<string>? listaOpcoes = null;
                if (!string.IsNullOrWhiteSpace(opcoesTexto))
                {
                    listaOpcoes = opcoesTexto.Split(',')
                        .Select(o => o.Trim())
                        .Where(o => !string.IsNullOrEmpty(o))
                        .ToList();
                }

                // Obtém a última ordem
                var formulario = await _contexto.Formularios
                    .Include(f => f.Campos)
                    .FirstOrDefaultAsync(f => f.Id == formularioId);

                int ordem = (formulario?.Campos.Count ?? 0) + 1;

                await _servicoFormulario.AdicionarCampoAsync(
                    formularioId,
                    label,
                    tipo,
                    obrigatorio,
                    ordem,
                    dicaAjuda,
                    listaOpcoes
                );

                TempData["Sucesso"] = "Campo adicionado com sucesso!";
            }
            catch (Exception ex)
            {
                TempData["Erro"] = $"Erro ao adicionar campo: {ex.Message}";
            }

            return RedirectToAction(nameof(Criar), new { id = formularioId });
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
