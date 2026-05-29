using System;
using SistemaWorkflow.Dominio.Comum;

namespace SistemaWorkflow.Dominio.Entidades
{
    public class Anexo : EntidadeBase
    {
        public Guid InstanciaProcessoId { get; set; }
        public virtual InstanciaProcesso InstanciaProcesso { get; set; } = null!;

        public Guid? TarefaProcessoId { get; set; }
        public virtual TarefaProcesso? TarefaProcesso { get; set; }

        public string NomeArquivo { get; set; } = string.Empty;
        public string CaminhoArquivo { get; set; } = string.Empty;
        public long TamanhoBytes { get; set; }
        public string TipoConteudo { get; set; } = string.Empty; // ex: "image/png"

        public Guid UsuarioUploadId { get; set; }
        public virtual Usuario UsuarioUpload { get; set; } = null!;
    }
}
