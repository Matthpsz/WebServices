using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using SistemaWorkflow.Dominio.Entidades;
using SistemaWorkflow.Dominio.Enums;

namespace SistemaWorkflow.Aplicacao.Interfaces
{
    public interface IServicoFormulario
    {
        Task<Formulario> CriarFormularioAsync(string nome, string descricao);
        
        Task<CampoFormulario> AdicionarCampoAsync(
            Guid formularioId, 
            string label, 
            TipoCampoFormulario tipo, 
            bool obrigatorio, 
            int ordem, 
            string dicaAjuda = null, 
            List<string> opcoes = null);

        Task<Formulario> ObterPorIdAsync(Guid formularioId);
        
        Task<List<Formulario>> ObterTodosAsync();
    }
}
