using System.Collections.Generic;
using SistemaWorkflow.Dominio.Comum;

namespace SistemaWorkflow.Dominio.Entidades
{
    public class Formulario : EntidadeBase
    {
        public string Nome { get; set; } = string.Empty;
        public string Descricao { get; set; } = string.Empty;

        // Relacionamentos
        public virtual ICollection<CampoFormulario> Campos { get; set; } = new List<CampoFormulario>();
        public virtual ICollection<Atividade> Atividades { get; set; } = new List<Atividade>();
    }
}
