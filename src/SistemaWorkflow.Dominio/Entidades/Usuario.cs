using System.Collections.Generic;
using SistemaWorkflow.Dominio.Comum;

namespace SistemaWorkflow.Dominio.Entidades
{
    public class Usuario : EntidadeBase
    {
        public string Nome { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string SenhaHash { get; set; } = string.Empty;

        // Relacionamentos
        public virtual ICollection<UsuarioPerfil> UsuarioPerfis { get; set; } = new List<UsuarioPerfil>();
        public virtual ICollection<EquipeUsuario> EquipeUsuarios { get; set; } = new List<EquipeUsuario>();
        public virtual ICollection<HistoricoTarefa> HistoricoTarefas { get; set; } = new List<HistoricoTarefa>();
        public virtual ICollection<Comentario> Comentarios { get; set; } = new List<Comentario>();
    }
}
