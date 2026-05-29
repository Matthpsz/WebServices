using System;
using SistemaWorkflow.Dominio.Comum;

namespace SistemaWorkflow.Dominio.Entidades
{
    public class UsuarioPerfil : EntidadeBase
    {
        public Guid UsuarioId { get; set; }
        public virtual Usuario Usuario { get; set; } = null!;

        public Guid PerfilId { get; set; }
        public virtual Perfil Perfil { get; set; } = null!;
    }
}
