using System;
using System.Collections.Generic;
using SistemaWorkflow.Dominio.Comum;
using SistemaWorkflow.Dominio.Enums;

namespace SistemaWorkflow.Dominio.Entidades
{
    public class CampoFormulario : EntidadeBase
    {
        public Guid FormularioId { get; set; }
        public virtual Formulario Formulario { get; set; } = null!;

        public string Nome { get; set; } = string.Empty; // ex: "limite_credito"
        public string Rotulo { get; set; } = string.Empty; // ex: "Limite de Crédito"
        public TipoCampoFormulario Tipo { get; set; }
        public bool Obrigatorio { get; set; } = false;
        public int Ordem { get; set; }

        // Relacionamentos
        public virtual ICollection<OpcaoCampo> Opcoes { get; set; } = new List<OpcaoCampo>();
        public virtual ICollection<ValorCampo> Valores { get; set; } = new List<ValorCampo>();
    }
}
