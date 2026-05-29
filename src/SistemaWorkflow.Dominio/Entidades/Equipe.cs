using System.Collections.Generic;
using SistemaWorkflow.Dominio.Comum;

namespace SistemaWorkflow.Dominio.Entidades
{
    public class Equipe : EntidadeBase
    {
        public string Nome { get; set; } = string.Empty;
        public string Descricao { get; set; } = string.Empty;

        // Relacionamentos
        public virtual ICollection<EquipeUsuario> EquipeUsuarios { get; set; } = new List<EquipeUsuario>();
        public virtual ICollection<Processo> Processos { get; set; } = new List<Processo>();
    }
}
