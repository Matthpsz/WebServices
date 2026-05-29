using System;
using System.Collections.Generic;
using SistemaWorkflow.Dominio.Comum;

namespace SistemaWorkflow.Dominio.Entidades
{
    public class Processo : EntidadeBase
    {
        public string Nome { get; set; } = string.Empty;
        public string Descricao { get; set; } = string.Empty;

        public Guid EquipeId { get; set; }
        public virtual Equipe Equipe { get; set; } = null!;

        // Relacionamentos
        public virtual ICollection<VersaoProcesso> Versoes { get; set; } = new List<VersaoProcesso>();
    }
}
