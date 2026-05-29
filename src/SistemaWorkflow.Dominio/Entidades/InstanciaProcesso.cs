using System;
using System.Collections.Generic;
using SistemaWorkflow.Dominio.Comum;
using SistemaWorkflow.Dominio.Enums;

namespace SistemaWorkflow.Dominio.Entidades
{
    public class InstanciaProcesso : EntidadeBase
    {
        public Guid VersaoProcessoId { get; set; }
        public virtual VersaoProcesso VersaoProcesso { get; set; } = null!;

        public string CodigoIdentificador { get; set; } = string.Empty; // ex: "WF-2026-0001"
        public StatusInstanciaProcesso Status { get; set; } = StatusInstanciaProcesso.Ativa;

        public Guid UsuarioCriadorId { get; set; }
        public virtual Usuario UsuarioCriador { get; set; } = null!;

        public DateTime DataInicio { get; set; } = DateTime.UtcNow;
        public DateTime? DataFim { get; set; }

        // Relacionamentos
        public virtual ICollection<TarefaProcesso> Tarefas { get; set; } = new List<TarefaProcesso>();
        public virtual ICollection<ValorCampo> ValoresCampos { get; set; } = new List<ValorCampo>();
        public virtual ICollection<Comentario> Comentarios { get; set; } = new List<Comentario>();
        public virtual ICollection<Anexo> Anexos { get; set; } = new List<Anexo>();
    }
}
