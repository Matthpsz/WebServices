using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using SistemaWorkflow.Dominio.Entidades;

namespace SistemaWorkflow.Aplicacao.Interfaces
{
    public interface IServicoProcesso
    {
        Task<Processo> CriarProcessoAsync(string nome, string descricao, Guid equipeId);
        
        Task<VersaoProcesso> CriarVersaoProcessoAsync(Guid processoId);
        
        Task<Atividade> AdicionarAtividadeAsync(
            Guid versaoProcessoId,
            string nome,
            string descricao,
            int slaMinutos,
            Guid? responsavelUsuarioId,
            Guid? responsavelEquipeId,
            Guid? formularioId,
            bool atividadeInicial = false,
            bool atividadeFinal = false);

        Task<TransicaoAtividade> AdicionarTransicaoAsync(
            Guid versaoProcessoId,
            Guid atividadeOrigemId,
            Guid atividadeDestinoId,
            string condicao,
            string nomeAcao);

        Task<VersaoProcesso> HomologarVersaoAsync(
            Guid versaoProcessoId, 
            Guid usuarioAdminId, 
            bool aprovado, 
            string justificativa);

        Task<VersaoProcesso> ObterVersaoCompletaAsync(Guid versaoId);
        
        Task<List<Processo>> ObterTodosProcessosAsync();
    }
}
