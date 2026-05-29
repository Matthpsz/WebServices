using System;
using System.Collections.Generic;
using SistemaWorkflow.Dominio.Comum;
using SistemaWorkflow.Dominio.Enums;

namespace SistemaWorkflow.Dominio.Entidades
{
    public class VersaoProcesso : EntidadeBase
    {
        public Guid ProcessoId { get; set; }
        public virtual Processo Processo { get; set; } = null!;

        public int NumeroVersao { get; set; }
        public StatusVersaoProcesso Status { get; set; } = StatusVersaoProcesso.Rascunho;

        // Fluxo de Homologação de Processos (Validação do Admin)
        public Guid? UsuarioValidadorId { get; set; }
        public virtual Usuario? UsuarioValidador { get; set; }

        public DateTime? DataValidacao { get; set; }
        public string? JustificativaValidacao { get; set; }

        // Relacionamentos
        public virtual ICollection<Atividade> Atividades { get; set; } = new List<Atividade>();
        public virtual ICollection<InstanciaProcesso> Instancias { get; set; } = new List<InstanciaProcesso>();
    }
}
