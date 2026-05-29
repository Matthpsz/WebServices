using System;
using System.Collections.Generic;
using SistemaWorkflow.Dominio.Comum;
using SistemaWorkflow.Dominio.Enums;

namespace SistemaWorkflow.Dominio.Entidades
{
    public class TarefaProcesso : EntidadeBase
    {
        public Guid InstanciaProcessoId { get; set; }
        public virtual InstanciaProcesso InstanciaProcesso { get; set; } = null!;

        public Guid AtividadeId { get; set; }
        public virtual Atividade Atividade { get; set; } = null!;

        public StatusTarefaProcesso Status { get; set; } = StatusTarefaProcesso.Pendente;

        // Atribuídos/Responsáveis
        public Guid? UsuarioResponsavelId { get; set; }
        public virtual Usuario? UsuarioResponsavel { get; set; }

        public Guid? EquipeResponsavelId { get; set; }
        public virtual Equipe? EquipeResponsavel { get; set; }

        // SLAs e Conclusão
        public DateTime? DataLimite { get; set; } // SLA Limite
        public DateTime? DataConclusao { get; set; }

        public Guid? UsuarioConclusorId { get; set; }
        public virtual Usuario? UsuarioConclusor { get; set; }

        public string? AcaoExecutada { get; set; } // ex: "Aprovar", "Rejeitar", "Retornar"
        public string? Justificativa { get; set; } // ex: "Orçamento aprovado"

        // Relacionamentos
        public virtual ICollection<ValorCampo> ValoresCampos { get; set; } = new List<ValorCampo>();
        public virtual ICollection<HistoricoTarefa> Historicos { get; set; } = new List<HistoricoTarefa>();
    }
}
