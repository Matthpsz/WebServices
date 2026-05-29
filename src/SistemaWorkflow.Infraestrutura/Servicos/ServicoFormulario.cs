using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using SistemaWorkflow.Aplicacao.Interfaces;
using SistemaWorkflow.Dominio.Entidades;
using SistemaWorkflow.Dominio.Enums;
using SistemaWorkflow.Infraestrutura.BancoDados;

namespace SistemaWorkflow.Infraestrutura.Servicos
{
    public class ServicoFormulario : IServicoFormulario
    {
        private readonly ContextoWorkflow _contexto;

        public ServicoFormulario(ContextoWorkflow contexto)
        {
            _contexto = contexto;
        }

        public async Task<Formulario> CriarFormularioAsync(string nome, string descricao)
        {
            if (string.IsNullOrWhiteSpace(nome))
                throw new ArgumentException("O nome do formulário não pode ser vazio.", nameof(nome));

            var formulario = new Formulario
            {
                Nome = nome,
                Descricao = descricao,
                Ativo = true
            };

            _contexto.Formularios.Add(formulario);
            await _contexto.SaveChangesAsync();

            return formulario;
        }

        public async Task<CampoFormulario> AdicionarCampoAsync(
            Guid formularioId, 
            string label, 
            TipoCampoFormulario tipo, 
            bool obrigatorio, 
            int ordem, 
            string dicaAjuda = null, 
            List<string> opcoes = null)
        {
            if (string.IsNullOrWhiteSpace(label))
                throw new ArgumentException("O rótulo (label) do campo não pode ser vazio.", nameof(label));

            var formularioExiste = await _contexto.Formularios.AnyAsync(f => f.Id == formularioId);
            if (!formularioExiste)
                throw new KeyNotFoundException("Formulário não encontrado.");

            var campo = new CampoFormulario
            {
                FormularioId = formularioId,
                Nome = label.Trim().ToLower().Replace(" ", "_"),
                Rotulo = label.Trim(),
                Tipo = tipo,
                Obrigatorio = obrigatorio,
                Ordem = ordem,
                Ativo = true
            };

            _contexto.CamposFormulario.Add(campo);
            await _contexto.SaveChangesAsync();

            // Adiciona opções caso o tipo do campo exija (ex: Selecao)
            if (tipo == TipoCampoFormulario.Selecao && opcoes != null && opcoes.Any())
            {
                var ordemOpcao = 1;
                foreach (var valorOpcao in opcoes)
                {
                    if (string.IsNullOrWhiteSpace(valorOpcao)) continue;

                    var opcao = new OpcaoCampo
                    {
                        CampoFormularioId = campo.Id,
                        Valor = valorOpcao.Trim(),
                        TextoExibicao = valorOpcao.Trim(),
                        Ordem = ordemOpcao++
                    };
                    _contexto.OpcoesCampo.Add(opcao);
                }
                await _contexto.SaveChangesAsync();
            }

            return campo;
        }

        public async Task<Formulario> ObterPorIdAsync(Guid formularioId)
        {
            var formulario = await _contexto.Formularios
                .Include(f => f.Campos.Where(c => c.Ativo))
                    .ThenInclude(c => c.Opcoes)
                .FirstOrDefaultAsync(f => f.Id == formularioId && f.Ativo);

            if (formulario == null)
                throw new KeyNotFoundException("Formulário não encontrado.");

            // Ordena os campos por ordem definida
            formulario.Campos = formulario.Campos.OrderBy(c => c.Ordem).ToList();

            return formulario;
        }

        public async Task<List<Formulario>> ObterTodosAsync()
        {
            return await _contexto.Formularios
                .Where(f => f.Ativo)
                .OrderBy(f => f.Nome)
                .ToListAsync();
        }
    }
}
