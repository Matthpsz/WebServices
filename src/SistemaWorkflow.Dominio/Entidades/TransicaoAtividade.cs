using System;
using SistemaWorkflow.Dominio.Comum;

namespace SistemaWorkflow.Dominio.Entidades
{
    public class TransicaoAtividade : EntidadeBase
    {
        public Guid VersaoProcessoId { get; set; }
        public virtual VersaoProcesso VersaoProcesso { get; set; } = null!;

        public Guid AtividadeOrigemId { get; set; }
        public virtual Atividade AtividadeOrigem { get; set; } = null!;

        public Guid AtividadeDestinoId { get; set; }
        public virtual Atividade AtividadeDestino { get; set; } = null!;

        public string Nome { get; set; } = string.Empty; // ex: "Aprovado", "Rejeitado"
        public string? Condicao { get; set; } // Expressão condicional (ex: "moeda_valor > 1000")
    }
}
