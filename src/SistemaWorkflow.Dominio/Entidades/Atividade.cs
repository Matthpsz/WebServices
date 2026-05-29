using System;
using System.Collections.Generic;
using SistemaWorkflow.Dominio.Comum;

namespace SistemaWorkflow.Dominio.Entidades
{
    public class Atividade : EntidadeBase
    {
        public Guid VersaoProcessoId { get; set; }
        public virtual VersaoProcesso VersaoProcesso { get; set; } = null!;

        public string Nome { get; set; } = string.Empty;
        public string Descricao { get; set; } = string.Empty;
        public int SlaMinutos { get; set; } // SLA em minutos
        public int Ordem { get; set; }

        public bool EhInicial { get; set; } = false;
        public bool EhFinal { get; set; } = false;

        // Responsáveis (Pode ser Usuário, Equipe ou Ambos/Qualquer um dos dois)
        public Guid? ResponsavelUsuarioId { get; set; }
        public virtual Usuario? ResponsavelUsuario { get; set; }

        public Guid? ResponsavelEquipeId { get; set; }
        public virtual Equipe? ResponsavelEquipe { get; set; }

        // Formulário Próprio da Etapa
        public Guid? FormularioId { get; set; }
        public virtual Formulario? Formulario { get; set; }

        // Relacionamentos
        public virtual ICollection<TransicaoAtividade> TransicoesOrigem { get; set; } = new List<TransicaoAtividade>();
        public virtual ICollection<TransicaoAtividade> TransicoesDestino { get; set; } = new List<TransicaoAtividade>();
        public virtual ICollection<TarefaProcesso> Tarefas { get; set; } = new List<TarefaProcesso>();
    }
}
