using System;
using SistemaWorkflow.Dominio.Comum;

namespace SistemaWorkflow.Dominio.Entidades
{
    public class PerfilPermissao : EntidadeBase
    {
        public Guid PerfilId { get; set; }
        public virtual Perfil Perfil { get; set; } = null!;

        public Guid PermissaoId { get; set; }
        public virtual Permissao Permissao { get; set; } = null!;
    }
}
