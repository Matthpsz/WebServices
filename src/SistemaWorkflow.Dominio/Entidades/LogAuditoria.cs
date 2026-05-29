using System;
using SistemaWorkflow.Dominio.Comum;

namespace SistemaWorkflow.Dominio.Entidades
{
    public class LogAuditoria : EntidadeBase
    {
        public Guid? UsuarioId { get; set; }
        public virtual Usuario? Usuario { get; set; }

        public string Entidade { get; set; } = string.Empty; // ex: "Processos"
        public string ChaveEntidade { get; set; } = string.Empty; // ex: ID em string
        public string Acao { get; set; } = string.Empty; // ex: "Insercao", "Alteracao", "Exclusao"
        public string? ValoresAntigos { get; set; } // JSON
        public string? ValoresNovos { get; set; } // JSON
        public DateTime DataHora { get; set; } = DateTime.UtcNow;
    }
}
