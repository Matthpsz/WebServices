using System;
using SistemaWorkflow.Dominio.Comum;

namespace SistemaWorkflow.Dominio.Entidades
{
    public class HistoricoTarefa : EntidadeBase
    {
        public Guid TarefaProcessoId { get; set; }
        public virtual TarefaProcesso TarefaProcesso { get; set; } = null!;

        public Guid? UsuarioExecutorId { get; set; }
        public virtual Usuario? UsuarioExecutor { get; set; }

        public DateTime DataAcao { get; set; } = DateTime.UtcNow;
        public string Acao { get; set; } = string.Empty; // ex: "Criou", "Assumiu", "Concluiu"
        public string Detalhes { get; set; } = string.Empty;
    }
}
