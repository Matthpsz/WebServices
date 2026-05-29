using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using SistemaWorkflow.Dominio.Entidades;

namespace SistemaWorkflow.Aplicacao.Interfaces
{
    public interface IMotorWorkflow
    {
        Task<InstanciaProcesso> IniciarInstanciaAsync(Guid versaoProcessoId, Guid usuarioCriadorId, string titulo = null);
        
        Task CompletarTarefaAsync(
            Guid tarefaId, 
            Guid usuarioExecutorId, 
            Dictionary<Guid, string> valoresCampos, 
            string nomeAcaoTransicao = null);
    }
}
