using System;
using SistemaWorkflow.Dominio.Comum;

namespace SistemaWorkflow.Dominio.Entidades
{
    public class EquipeUsuario : EntidadeBase
    {
        public Guid EquipeId { get; set; }
        public virtual Equipe Equipe { get; set; } = null!;

        public Guid UsuarioId { get; set; }
        public virtual Usuario Usuario { get; set; } = null!;

        public bool AdministradorEquipe { get; set; } = false; // Identifica o Chefe de Equipe
    }
}
