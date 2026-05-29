using System.Collections.Generic;
using SistemaWorkflow.Dominio.Comum;

namespace SistemaWorkflow.Dominio.Entidades
{
    public class Permissao : EntidadeBase
    {
        public string Nome { get; set; } = string.Empty;
        public string Chave { get; set; } = string.Empty;
        public string Descricao { get; set; } = string.Empty;

        // Relacionamentos
        public virtual ICollection<PerfilPermissao> PerfilPermissoes { get; set; } = new List<PerfilPermissao>();
    }
}
