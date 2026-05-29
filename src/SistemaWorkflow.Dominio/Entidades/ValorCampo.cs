using System;
using SistemaWorkflow.Dominio.Comum;

namespace SistemaWorkflow.Dominio.Entidades
{
    public class ValorCampo : EntidadeBase
    {
        public Guid InstanciaProcessoId { get; set; }
        public virtual InstanciaProcesso InstanciaProcesso { get; set; } = null!;

        public Guid CampoFormularioId { get; set; }
        public virtual CampoFormulario CampoFormulario { get; set; } = null!;

        public Guid? TarefaProcessoId { get; set; }
        public virtual TarefaProcesso? TarefaProcesso { get; set; }

        public string Valor { get; set; } = string.Empty; // Valor serializado como string
    }
}
