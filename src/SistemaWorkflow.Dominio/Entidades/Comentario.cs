using System;
using SistemaWorkflow.Dominio.Comum;

namespace SistemaWorkflow.Dominio.Entidades
{
    public class Comentario : EntidadeBase
    {
        public Guid InstanciaProcessoId { get; set; }
        public virtual InstanciaProcesso InstanciaProcesso { get; set; } = null!;

        public Guid UsuarioId { get; set; }
        public virtual Usuario Usuario { get; set; } = null!;

        public string Texto { get; set; } = string.Empty;
        public DateTime DataHora { get; set; } = DateTime.UtcNow;
    }
}
