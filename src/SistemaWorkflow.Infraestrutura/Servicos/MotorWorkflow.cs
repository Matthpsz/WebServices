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
    public class MotorWorkflow : IMotorWorkflow
    {
        private readonly ContextoWorkflow _contexto;

        public MotorWorkflow(ContextoWorkflow contexto)
        {
            _contexto = contexto;
        }

        /// <summary>
        /// Inicia uma nova instância de processo com base em uma versão ativa e homologada.
        /// </summary>
        public async Task<InstanciaProcesso> IniciarInstanciaAsync(Guid versaoProcessoId, Guid usuarioCriadorId, string titulo = null)
        {
            var versao = await _contexto.VersoesProcesso
                .Include(v => v.Processo)
                .Include(v => v.Atividades)
                .FirstOrDefaultAsync(v => v.Id == versaoProcessoId && v.Ativo);

            if (versao == null)
                throw new KeyNotFoundException("Versão de processo não encontrada.");

            if (versao.Status != StatusVersaoProcesso.Ativo)
                throw new InvalidOperationException("Apenas versões homologadas e ativas do processo podem ser iniciadas.");

            var atividadeInicial = versao.Atividades.FirstOrDefault(a => a.EhInicial && a.Ativo);
            if (atividadeInicial == null)
                throw new InvalidOperationException("Esta versão de processo não possui uma atividade inicial configurada.");

            // Gerar código único e incrementado: ex: BPM-2026-0001
            var anoAtual = DateTime.Now.Year;
            var prefixo = $"BPM-{anoAtual}-";
            var totalAno = await _contexto.InstanciasProcesso
                .CountAsync(i => i.CodigoIdentificador.StartsWith(prefixo));
            var proximoNumero = totalAno + 1;
            var codigoIdentificador = $"{prefixo}{proximoNumero:D4}";

            var instancia = new InstanciaProcesso
            {
                VersaoProcessoId = versaoProcessoId,
                CodigoIdentificador = codigoIdentificador,
                Status = StatusInstanciaProcesso.Ativa,
                UsuarioCriadorId = usuarioCriadorId,
                Ativo = true
            };

            _contexto.InstanciasProcesso.Add(instancia);
            await _contexto.SaveChangesAsync();

            // Cria a primeira tarefa para a atividade inicial
            var tarefaInicial = new TarefaProcesso
            {
                InstanciaProcessoId = instancia.Id,
                AtividadeId = atividadeInicial.Id,
                Status = StatusTarefaProcesso.Pendente,
                UsuarioResponsavelId = atividadeInicial.ResponsavelUsuarioId ?? usuarioCriadorId, // se não definir responsável, assume o criador
                EquipeResponsavelId = atividadeInicial.ResponsavelEquipeId,
                Ativo = true
            };

            _contexto.TarefasProcesso.Add(tarefaInicial);
            await _contexto.SaveChangesAsync();

            // Registra no histórico de tarefas
            var historico = new HistoricoTarefa
            {
                TarefaProcessoId = tarefaInicial.Id,
                Acao = "Instanciado e Iniciado",
                UsuarioExecutorId = usuarioCriadorId,
                DataAcao = DateTime.UtcNow,
                Detalhes = $"Instância iniciada sob título: {titulo ?? versao.Processo.Nome}"
            };

            _contexto.HistoricoTarefas.Add(historico);
            await _contexto.SaveChangesAsync();

            return instancia;
        }

        /// <summary>
        /// Conclui uma tarefa de workflow ativa, validando e salvando as respostas do formulário dinâmico,
        /// e executando a transição para a próxima atividade ou finalização do processo.
        /// </summary>
        public async Task CompletarTarefaAsync(
            Guid tarefaId, 
            Guid usuarioExecutorId, 
            Dictionary<Guid, string> valoresCampos, 
            string nomeAcaoTransicao = null)
        {
            var tarefa = await _contexto.TarefasProcesso
                .Include(t => t.InstanciaProcesso)
                .Include(t => t.Atividade)
                    .ThenInclude(a => a.Formulario)
                        .ThenInclude(f => f.Campos)
                .FirstOrDefaultAsync(t => t.Id == tarefaId && t.Ativo);

            if (tarefa == null)
                throw new KeyNotFoundException("Tarefa de processo não encontrada.");

            if (tarefa.Status != StatusTarefaProcesso.Pendente)
                throw new InvalidOperationException("Apenas tarefas com status Pendente podem ser concluídas.");

            // 1. Validação de Formulário Dinâmico
            if (tarefa.Atividade.Formulario != null)
            {
                var campos = tarefa.Atividade.Formulario.Campos.Where(c => c.Ativo).ToList();
                foreach (var campo in campos)
                {
                    valoresCampos.TryGetValue(campo.Id, out var valorEnviado);
                    var valorTratado = valorEnviado?.Trim();

                    // Valida obrigatoriedade
                    if (campo.Obrigatorio && string.IsNullOrWhiteSpace(valorTratado))
                    {
                        throw new ArgumentException($"O campo '{campo.Rotulo}' é de preenchimento obrigatório.");
                    }

                    // Valida formatos específicos com base no Tipo do Campo
                    if (!string.IsNullOrWhiteSpace(valorTratado))
                    {
                        switch (campo.Tipo)
                        {
                            case TipoCampoFormulario.Numero:
                                if (!decimal.TryParse(valorTratado, out _))
                                    throw new ArgumentException($"O campo '{campo.Rotulo}' deve conter um número válido.");
                                break;
                            case TipoCampoFormulario.Data:
                                if (!DateTime.TryParse(valorTratado, out _))
                                    throw new ArgumentException($"O campo '{campo.Rotulo}' deve conter uma data válida.");
                                break;
                            case TipoCampoFormulario.Booleano:
                                if (valorTratado != "true" && valorTratado != "false")
                                    throw new ArgumentException($"O campo '{campo.Rotulo}' deve ser verdadeiro ou falso.");
                                break;
                        }
                    }

                    // Salva o valor no banco
                    var valorCampo = new ValorCampo
                    {
                        InstanciaProcessoId = tarefa.InstanciaProcessoId,
                        TarefaProcessoId = tarefa.Id,
                        CampoFormularioId = campo.Id,
                        Valor = valorTratado,
                        Ativo = true
                    };
                    _contexto.ValoresCampo.Add(valorCampo);
                }
            }

            // 2. Conclui a tarefa atual
            tarefa.Status = StatusTarefaProcesso.Concluida;
            tarefa.DataConclusao = DateTime.UtcNow;
            tarefa.UsuarioConclusorId = usuarioExecutorId;

            // 3. Salva histórico
            var historico = new HistoricoTarefa
            {
                TarefaProcessoId = tarefa.Id,
                Acao = string.IsNullOrWhiteSpace(nomeAcaoTransicao) ? "Concluída" : $"Ação: {nomeAcaoTransicao}",
                UsuarioExecutorId = usuarioExecutorId,
                DataAcao = DateTime.UtcNow,
                Detalhes = "Etapa concluída pelo motor de workflow."
            };
            _contexto.HistoricoTarefas.Add(historico);

            // 4. Orquestra a transição de fluxo (BPM)
            var transicoes = await _contexto.TransicoesAtividade
                .Where(t => t.VersaoProcessoId == tarefa.InstanciaProcesso.VersaoProcessoId && t.AtividadeOrigemId == tarefa.AtividadeId && t.Ativo)
                .ToListAsync();

            TransicaoAtividade transicaoEscolhida = null;

            if (transicoes.Any())
            {
                if (!string.IsNullOrWhiteSpace(nomeAcaoTransicao))
                {
                    // Tenta encontrar transição que coincida com o nome da ação do usuário (Aprovar, Rejeitar, etc.)
                    transicaoEscolhida = transicoes.FirstOrDefault(t => 
                        string.Equals(t.Nome, nomeAcaoTransicao, StringComparison.OrdinalIgnoreCase));
                }

                // Fallback para a primeira transição se nenhuma foi explicitada ou encontrada
                transicaoEscolhida ??= transicoes.First();
            }

            if (transicaoEscolhida != null)
            {
                var atividadeDestino = await _contexto.Atividades
                    .FirstOrDefaultAsync(a => a.Id == transicaoEscolhida.AtividadeDestinoId && a.Ativo);

                if (atividadeDestino == null)
                    throw new InvalidOperationException("Atividade de destino da transição não encontrada.");

                if (atividadeDestino.EhFinal)
                {
                    // Finaliza a instância do processo
                    tarefa.InstanciaProcesso.Status = StatusInstanciaProcesso.Concluida;
                    tarefa.InstanciaProcesso.DataFim = DateTime.UtcNow;
                }
                else
                {
                    // Cria uma nova tarefa pendente
                    var novaTarefa = new TarefaProcesso
                    {
                        InstanciaProcessoId = tarefa.InstanciaProcessoId,
                        AtividadeId = atividadeDestino.Id,
                        Status = StatusTarefaProcesso.Pendente,
                        UsuarioResponsavelId = atividadeDestino.ResponsavelUsuarioId,
                        EquipeResponsavelId = atividadeDestino.ResponsavelEquipeId,
                        Ativo = true
                    };
                    _contexto.TarefasProcesso.Add(novaTarefa);
                }
            }
            else
            {
                // Se não houver mais transições configuradas, finaliza o processo
                tarefa.InstanciaProcesso.Status = StatusInstanciaProcesso.Concluida;
                tarefa.InstanciaProcesso.DataFim = DateTime.UtcNow;
            }

            await _contexto.SaveChangesAsync();
        }
    }
}
