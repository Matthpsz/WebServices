using System;
using SistemaWorkflow.Dominio.Comum;

namespace SistemaWorkflow.Dominio.Entidades
{
    public class OpcaoCampo : EntidadeBase
    {
        public Guid CampoFormularioId { get; set; }
        public virtual CampoFormulario CampoFormulario { get; set; } = null!;

        public string Valor { get; set; } = string.Empty; // ex: "sp"
        public string TextoExibicao { get; set; } = string.Empty; // ex: "São Paulo"
        public int Ordem { get; set; }
    }
}
