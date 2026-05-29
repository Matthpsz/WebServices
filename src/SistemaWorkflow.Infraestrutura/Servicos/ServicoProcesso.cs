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
    public class ServicoProcesso : IServicoProcesso
    {
        private readonly ContextoWorkflow _contexto;

        public ServicoProcesso(ContextoWorkflow contexto)
        {
            _contexto = contexto;
        }

        public async Task<Processo> CriarProcessoAsync(string nome, string descricao, Guid equipeId)
        {
            if (string.IsNullOrWhiteSpace(nome))
                throw new ArgumentException("O nome do processo não pode ser vazio.", nameof(nome));

            var equipeExiste = await _contexto.Equipes.AnyAsync(e => e.Id == equipeId);
            if (!equipeExiste)
                throw new KeyNotFoundException("Equipe responsável não encontrada.");

            var processo = new Processo
            {
                Nome = nome,
                Descricao = descricao,
                EquipeId = equipeId,
                Ativo = true
            };

            _contexto.Processos.Add(processo);
            await _contexto.SaveChangesAsync();

            return processo;
        }

        public async Task<VersaoProcesso> CriarVersaoProcessoAsync(Guid processoId)
        {
            var processo = await _contexto.Processos
                .Include(p => p.Versoes)
                .FirstOrDefaultAsync(p => p.Id == processoId && p.Ativo);

            if (processo == null)
                throw new KeyNotFoundException("Processo não encontrado ou inativo.");

            var proximoNumeroVersao = processo.Versoes.Any() 
                ? processo.Versoes.Max(v => v.NumeroVersao) + 1 
                : 1;

            var novaVersao = new VersaoProcesso
            {
                ProcessoId = processoId,
                NumeroVersao = proximoNumeroVersao,
                Status = StatusVersaoProcesso.Rascunho, // Sempre inicia como Rascunho
                Ativo = true
            };

            _contexto.VersoesProcesso.Add(novaVersao);
            await _contexto.SaveChangesAsync();

            return novaVersao;
        }

        public async Task<Atividade> AdicionarAtividadeAsync(
            Guid versaoProcessoId,
            string nome,
            string descricao,
            int slaMinutos,
            Guid? responsavelUsuarioId,
            Guid? responsavelEquipeId,
            Guid? formularioId,
            bool atividadeInicial = false,
            bool atividadeFinal = false)
        {
            if (string.IsNullOrWhiteSpace(nome))
                throw new ArgumentException("O nome do atividade não pode ser vazio.", nameof(nome));

            var versaoExiste = await _contexto.VersoesProcesso.AnyAsync(v => v.Id == versaoProcessoId);
            if (!versaoExiste)
                throw new KeyNotFoundException("Versão do processo não encontrada.");

            // Se for atividade inicial, garante que não haja outra atividade inicial ativa na mesma versão
            if (atividadeInicial)
            {
                var outraInicial = await _contexto.Atividades
                    .AnyAsync(a => a.VersaoProcessoId == versaoProcessoId && a.EhInicial && a.Ativo);
                if (outraInicial)
                    throw new InvalidOperationException("Esta versão já possui uma atividade inicial configurada.");
            }

            var atividade = new Atividade
            {
                VersaoProcessoId = versaoProcessoId,
                Nome = nome,
                Descricao = descricao,
                SlaMinutos = slaMinutos,
                ResponsavelUsuarioId = responsavelUsuarioId,
                ResponsavelEquipeId = responsavelEquipeId,
                FormularioId = formularioId,
                EhInicial = atividadeInicial,
                EhFinal = atividadeFinal,
                Ativo = true
            };

            _contexto.Atividades.Add(atividade);
            await _contexto.SaveChangesAsync();

            return atividade;
        }

        public async Task<TransicaoAtividade> AdicionarTransicaoAsync(
            Guid versaoProcessoId,
            Guid atividadeOrigemId,
            Guid atividadeDestinoId,
            string condicao,
            string nomeAcao)
        {
            if (string.IsNullOrWhiteSpace(nomeAcao))
                throw new ArgumentException("O nome da ação da transição não pode ser vazio.", nameof(nomeAcao));

            var versaoExiste = await _contexto.VersoesProcesso.AnyAsync(v => v.Id == versaoProcessoId);
            if (!versaoExiste)
                throw new KeyNotFoundException("Versão do processo não encontrada.");

            var origemExiste = await _contexto.Atividades.AnyAsync(a => a.Id == atividadeOrigemId && a.VersaoProcessoId == versaoProcessoId);
            var destinoExiste = await _contexto.Atividades.AnyAsync(a => a.Id == atividadeDestinoId && a.VersaoProcessoId == versaoProcessoId);

            if (!origemExiste || !destinoExiste)
                throw new KeyNotFoundException("Uma ou ambas as atividades não pertencem a esta versão do processo.");

            var transicao = new TransicaoAtividade
            {
                VersaoProcessoId = versaoProcessoId,
                AtividadeOrigemId = atividadeOrigemId,
                AtividadeDestinoId = atividadeDestinoId,
                Condicao = condicao?.Trim(),
                Nome = nomeAcao.Trim(),
                Ativo = true
            };

            _contexto.TransicoesAtividade.Add(transicao);
            await _contexto.SaveChangesAsync();

            return transicao;
        }

        public async Task<VersaoProcesso> HomologarVersaoAsync(
            Guid versaoProcessoId, 
            Guid usuarioAdminId, 
            bool aprovado, 
            string justificativa)
        {
            var versao = await _contexto.VersoesProcesso
                .Include(v => v.Processo)
                .FirstOrDefaultAsync(v => v.Id == versaoProcessoId && v.Ativo);

            if (versao == null)
                throw new KeyNotFoundException("Versão do processo não encontrada.");

            if (versao.Status != StatusVersaoProcesso.Rascunho)
                throw new InvalidOperationException("Apenas versões em rascunho podem ser homologadas.");

            var admin = await _contexto.Usuarios
                .Include(u => u.UsuarioPerfis)
                    .ThenInclude(up => up.Perfil)
                .FirstOrDefaultAsync(u => u.Id == usuarioAdminId && u.Ativo);

            if (admin == null)
                throw new KeyNotFoundException("Usuário homologador não encontrado.");

            var ehAdmin = admin.UsuarioPerfis.Any(up => up.Perfil.Nome == "Administrador");
            if (!ehAdmin)
                throw new UnauthorizedAccessException("Apenas usuários com o perfil de Administrador podem homologar processos.");

            if (aprovado)
            {
                // Inativa qualquer outra versão ativa deste processo para manter apenas uma versão de produção ativa
                var versoesAtivas = await _contexto.VersoesProcesso
                    .Where(v => v.ProcessoId == versao.ProcessoId && v.Status == StatusVersaoProcesso.Ativo && v.Ativo)
                    .ToListAsync();

                foreach (var vAtiva in versoesAtivas)
                {
                    vAtiva.Status = StatusVersaoProcesso.Inativo;
                }

                versao.Status = StatusVersaoProcesso.Ativo;
            }
            else
            {
                versao.Status = StatusVersaoProcesso.Rejeitado;
            }

            versao.UsuarioValidadorId = usuarioAdminId;
            versao.DataValidacao = DateTime.UtcNow;
            versao.JustificativaValidacao = justificativa;

            await _contexto.SaveChangesAsync();

            return versao;
        }

        public async Task<VersaoProcesso> ObterVersaoCompletaAsync(Guid versaoId)
        {
            var versao = await _contexto.VersoesProcesso
                .Include(v => v.Processo)
                .Include(v => v.Atividades.Where(a => a.Ativo))
                    .ThenInclude(a => a.Formulario)
                .FirstOrDefaultAsync(v => v.Id == versaoId && v.Ativo);

            if (versao == null)
                throw new KeyNotFoundException("Versão de processo não encontrada.");

            return versao;
        }

        public async Task<List<Processo>> ObterTodosProcessosAsync()
        {
            return await _contexto.Processos
                .Include(p => p.Equipe)
                .Include(p => p.Versoes)
                .Where(p => p.Ativo)
                .OrderBy(p => p.Nome)
                .ToListAsync();
        }
    }
}
