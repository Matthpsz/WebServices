using System;
using Microsoft.EntityFrameworkCore;
using SistemaWorkflow.Dominio.Entidades;

namespace SistemaWorkflow.Infraestrutura.BancoDados
{
    public class ContextoWorkflow : DbContext
    {
        public ContextoWorkflow(DbContextOptions<ContextoWorkflow> opcoes) : base(opcoes)
        {
        }

        // DbSets em português
        public DbSet<Usuario> Usuarios { get; set; } = null!;
        public DbSet<Perfil> Perfis { get; set; } = null!;
        public DbSet<Permissao> Permissoes { get; set; } = null!;
        public DbSet<PerfilPermissao> PerfilPermissoes { get; set; } = null!;
        public DbSet<UsuarioPerfil> UsuarioPerfis { get; set; } = null!;
        public DbSet<Equipe> Equipes { get; set; } = null!;
        public DbSet<EquipeUsuario> EquipeUsuarios { get; set; } = null!;
        public DbSet<Processo> Processos { get; set; } = null!;
        public DbSet<VersaoProcesso> VersoesProcesso { get; set; } = null!;
        public DbSet<Atividade> Atividades { get; set; } = null!;
        public DbSet<TransicaoAtividade> TransicoesAtividade { get; set; } = null!;
        public DbSet<InstanciaProcesso> InstanciasProcesso { get; set; } = null!;
        public DbSet<TarefaProcesso> TarefasProcesso { get; set; } = null!;
        public DbSet<HistoricoTarefa> HistoricoTarefas { get; set; } = null!;
        public DbSet<Formulario> Formularios { get; set; } = null!;
        public DbSet<CampoFormulario> CamposFormulario { get; set; } = null!;
        public DbSet<OpcaoCampo> OpcoesCampo { get; set; } = null!;
        public DbSet<ValorCampo> ValoresCampo { get; set; } = null!;
        public DbSet<LogAuditoria> LogsAuditoria { get; set; } = null!;
        public DbSet<Comentario> Comentarios { get; set; } = null!;
        public DbSet<Anexo> Anexos { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder construtor)
        {
            base.OnModelCreating(construtor);

            // 1. Segurança (RBAC)
            construtor.Entity<Usuario>(etidade =>
            {
                etidade.ToTable("Usuarios");
                etidade.HasKey(u => u.Id);
                etidade.Property(u => u.Nome).HasMaxLength(150).IsRequired();
                etidade.Property(u => u.Email).HasMaxLength(150).IsRequired();
                etidade.Property(u => u.SenhaHash).HasMaxLength(250).IsRequired();
                etidade.HasIndex(u => u.Email).IsUnique();
            });

            construtor.Entity<Perfil>(etidade =>
            {
                etidade.ToTable("Perfis");
                etidade.HasKey(p => p.Id);
                etidade.Property(p => p.Nome).HasMaxLength(50).IsRequired();
                etidade.Property(p => p.Descricao).HasMaxLength(250);
                etidade.HasIndex(p => p.Nome).IsUnique();
            });

            construtor.Entity<Permissao>(etidade =>
            {
                etidade.ToTable("Permissoes");
                etidade.HasKey(p => p.Id);
                etidade.Property(p => p.Nome).HasMaxLength(100).IsRequired();
                etidade.Property(p => p.Chave).HasMaxLength(100).IsRequired();
                etidade.Property(p => p.Descricao).HasMaxLength(250);
                etidade.HasIndex(p => p.Chave).IsUnique();
            });

            construtor.Entity<PerfilPermissao>(etidade =>
            {
                etidade.ToTable("PerfilPermissoes");
                etidade.HasKey(pp => pp.Id);
                etidade.HasOne(pp => pp.Perfil)
                    .WithMany(p => p.PerfilPermissoes)
                    .HasForeignKey(pp => pp.PerfilId)
                    .OnDelete(DeleteBehavior.Cascade);

                etidade.HasOne(pp => pp.Permissao)
                    .WithMany(p => p.PerfilPermissoes)
                    .HasForeignKey(pp => pp.PermissaoId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            construtor.Entity<UsuarioPerfil>(etidade =>
            {
                etidade.ToTable("UsuarioPerfis");
                etidade.HasKey(up => up.Id);
                etidade.HasOne(up => up.Usuario)
                    .WithMany(u => u.UsuarioPerfis)
                    .HasForeignKey(up => up.UsuarioId)
                    .OnDelete(DeleteBehavior.Cascade);

                etidade.HasOne(up => up.Perfil)
                    .WithMany(p => p.UsuarioPerfis)
                    .HasForeignKey(up => up.PerfilId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // 2. Organização
            construtor.Entity<Equipe>(etidade =>
            {
                etidade.ToTable("Equipes");
                etidade.HasKey(e => e.Id);
                etidade.Property(e => e.Nome).HasMaxLength(100).IsRequired();
                etidade.Property(e => e.Descricao).HasMaxLength(250);
            });

            construtor.Entity<EquipeUsuario>(etidade =>
            {
                etidade.ToTable("EquipeUsuarios");
                etidade.HasKey(eu => eu.Id);
                etidade.HasOne(eu => eu.Equipe)
                    .WithMany(e => e.EquipeUsuarios)
                    .HasForeignKey(eu => eu.EquipeId)
                    .OnDelete(DeleteBehavior.Cascade);

                etidade.HasOne(eu => eu.Usuario)
                    .WithMany(u => u.EquipeUsuarios)
                    .HasForeignKey(eu => eu.UsuarioId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // 3. Workflow (Definições)
            construtor.Entity<Processo>(etidade =>
            {
                etidade.ToTable("Processos");
                etidade.HasKey(p => p.Id);
                etidade.Property(p => p.Nome).HasMaxLength(100).IsRequired();
                etidade.Property(p => p.Descricao).HasMaxLength(500);
                etidade.HasOne(p => p.Equipe)
                    .WithMany(e => e.Processos)
                    .HasForeignKey(p => p.EquipeId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            construtor.Entity<VersaoProcesso>(etidade =>
            {
                etidade.ToTable("VersoesProcesso");
                etidade.HasKey(vp => vp.Id);
                etidade.Property(vp => vp.JustificativaValidacao).HasMaxLength(500);
                etidade.HasOne(vp => vp.Processo)
                    .WithMany(p => p.Versoes)
                    .HasForeignKey(vp => vp.ProcessoId)
                    .OnDelete(DeleteBehavior.Cascade);

                etidade.HasOne(vp => vp.UsuarioValidador)
                    .WithMany()
                    .HasForeignKey(vp => vp.UsuarioValidadorId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            construtor.Entity<Atividade>(etidade =>
            {
                etidade.ToTable("Atividades");
                etidade.HasKey(a => a.Id);
                etidade.Property(a => a.Nome).HasMaxLength(100).IsRequired();
                etidade.Property(a => a.Descricao).HasMaxLength(500);

                etidade.HasOne(a => a.VersaoProcesso)
                    .WithMany(vp => vp.Atividades)
                    .HasForeignKey(a => a.VersaoProcessoId)
                    .OnDelete(DeleteBehavior.Cascade);

                etidade.HasOne(a => a.ResponsavelUsuario)
                    .WithMany()
                    .HasForeignKey(a => a.ResponsavelUsuarioId)
                    .OnDelete(DeleteBehavior.Restrict);

                etidade.HasOne(a => a.ResponsavelEquipe)
                    .WithMany()
                    .HasForeignKey(a => a.ResponsavelEquipeId)
                    .OnDelete(DeleteBehavior.Restrict);

                etidade.HasOne(a => a.Formulario)
                    .WithMany(f => f.Atividades)
                    .HasForeignKey(a => a.FormularioId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            construtor.Entity<TransicaoAtividade>(etidade =>
            {
                etidade.ToTable("TransicoesAtividade");
                etidade.HasKey(ta => ta.Id);
                etidade.Property(ta => ta.Nome).HasMaxLength(100).IsRequired();
                etidade.Property(ta => ta.Condicao).HasMaxLength(500);

                etidade.HasOne(ta => ta.VersaoProcesso)
                    .WithMany()
                    .HasForeignKey(ta => ta.VersaoProcessoId)
                    .OnDelete(DeleteBehavior.Cascade);

                etidade.HasOne(ta => ta.AtividadeOrigem)
                    .WithMany(a => a.TransicoesOrigem)
                    .HasForeignKey(ta => ta.AtividadeOrigemId)
                    .OnDelete(DeleteBehavior.Restrict);

                etidade.HasOne(ta => ta.AtividadeDestino)
                    .WithMany(a => a.TransicoesDestino)
                    .HasForeignKey(ta => ta.AtividadeDestinoId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // 4. Execução
            construtor.Entity<InstanciaProcesso>(etidade =>
            {
                etidade.ToTable("InstanciasProcesso");
                etidade.HasKey(ip => ip.Id);
                etidade.Property(ip => ip.CodigoIdentificador).HasMaxLength(50).IsRequired();
                etidade.HasIndex(ip => ip.CodigoIdentificador).IsUnique();

                etidade.HasOne(ip => ip.VersaoProcesso)
                    .WithMany(vp => vp.Instancias)
                    .HasForeignKey(ip => ip.VersaoProcessoId)
                    .OnDelete(DeleteBehavior.Cascade);

                etidade.HasOne(ip => ip.UsuarioCriador)
                    .WithMany()
                    .HasForeignKey(ip => ip.UsuarioCriadorId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            construtor.Entity<TarefaProcesso>(etidade =>
            {
                etidade.ToTable("TarefasProcesso");
                etidade.HasKey(tp => tp.Id);
                etidade.Property(tp => tp.AcaoExecutada).HasMaxLength(100);
                etidade.Property(tp => tp.Justificativa).HasMaxLength(500);

                etidade.HasOne(tp => tp.InstanciaProcesso)
                    .WithMany(ip => ip.Tarefas)
                    .HasForeignKey(tp => tp.InstanciaProcessoId)
                    .OnDelete(DeleteBehavior.Cascade);

                etidade.HasOne(tp => tp.Atividade)
                    .WithMany(a => a.Tarefas)
                    .HasForeignKey(tp => tp.AtividadeId)
                    .OnDelete(DeleteBehavior.Restrict);

                etidade.HasOne(tp => tp.UsuarioResponsavel)
                    .WithMany()
                    .HasForeignKey(tp => tp.UsuarioResponsavelId)
                    .OnDelete(DeleteBehavior.Restrict);

                etidade.HasOne(tp => tp.EquipeResponsavel)
                    .WithMany()
                    .HasForeignKey(tp => tp.EquipeResponsavelId)
                    .OnDelete(DeleteBehavior.Restrict);

                etidade.HasOne(tp => tp.UsuarioConclusor)
                    .WithMany()
                    .HasForeignKey(tp => tp.UsuarioConclusorId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            construtor.Entity<HistoricoTarefa>(etidade =>
            {
                etidade.ToTable("HistoricoTarefas");
                etidade.HasKey(ht => ht.Id);
                etidade.Property(ht => ht.Acao).HasMaxLength(100).IsRequired();
                etidade.Property(ht => ht.Detalhes).HasMaxLength(500);

                etidade.HasOne(ht => ht.TarefaProcesso)
                    .WithMany(tp => tp.Historicos)
                    .HasForeignKey(ht => ht.TarefaProcessoId)
                    .OnDelete(DeleteBehavior.Cascade);

                etidade.HasOne(ht => ht.UsuarioExecutor)
                    .WithMany(u => u.HistoricoTarefas)
                    .HasForeignKey(ht => ht.UsuarioExecutorId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // 5. Formulários Dinâmicos
            construtor.Entity<Formulario>(etidade =>
            {
                etidade.ToTable("Formularios");
                etidade.HasKey(f => f.Id);
                etidade.Property(f => f.Nome).HasMaxLength(100).IsRequired();
                etidade.Property(f => f.Descricao).HasMaxLength(250);
            });

            construtor.Entity<CampoFormulario>(etidade =>
            {
                etidade.ToTable("CamposFormulario");
                etidade.HasKey(cf => cf.Id);
                etidade.Property(cf => cf.Nome).HasMaxLength(50).IsRequired();
                etidade.Property(cf => cf.Rotulo).HasMaxLength(100).IsRequired();

                etidade.HasOne(cf => cf.Formulario)
                    .WithMany(f => f.Campos)
                    .HasForeignKey(cf => cf.FormularioId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            construtor.Entity<OpcaoCampo>(etidade =>
            {
                etidade.ToTable("OpcoesCampo");
                etidade.HasKey(oc => oc.Id);
                etidade.Property(oc => oc.Valor).HasMaxLength(100).IsRequired();
                etidade.Property(oc => oc.TextoExibicao).HasMaxLength(100).IsRequired();

                etidade.HasOne(oc => oc.CampoFormulario)
                    .WithMany(cf => cf.Opcoes)
                    .HasForeignKey(oc => oc.CampoFormularioId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            construtor.Entity<ValorCampo>(etidade =>
            {
                etidade.ToTable("ValoresCampo");
                etidade.HasKey(vc => vc.Id);

                etidade.HasOne(vc => vc.InstanciaProcesso)
                    .WithMany(ip => ip.ValoresCampos)
                    .HasForeignKey(vc => vc.InstanciaProcessoId)
                    .OnDelete(DeleteBehavior.Cascade);

                etidade.HasOne(vc => vc.CampoFormulario)
                    .WithMany(cf => cf.Valores)
                    .HasForeignKey(vc => vc.CampoFormularioId)
                    .OnDelete(DeleteBehavior.Restrict);

                etidade.HasOne(vc => vc.TarefaProcesso)
                    .WithMany(tp => tp.ValoresCampos)
                    .HasForeignKey(vc => vc.TarefaProcessoId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // 6. Auditoria, Comentários e Anexos
            construtor.Entity<LogAuditoria>(etidade =>
            {
                etidade.ToTable("LogsAuditoria");
                etidade.HasKey(la => la.Id);
                etidade.Property(la => la.Entidade).HasMaxLength(100).IsRequired();
                etidade.Property(la => la.ChaveEntidade).HasMaxLength(100).IsRequired();
                etidade.Property(la => la.Acao).HasMaxLength(50).IsRequired();

                etidade.HasOne(la => la.Usuario)
                    .WithMany()
                    .HasForeignKey(la => la.UsuarioId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            construtor.Entity<Comentario>(etidade =>
            {
                etidade.ToTable("Comentarios");
                etidade.HasKey(c => c.Id);
                etidade.Property(c => c.Texto).HasMaxLength(1000).IsRequired();

                etidade.HasOne(c => c.InstanciaProcesso)
                    .WithMany(ip => ip.Comentarios)
                    .HasForeignKey(c => c.InstanciaProcessoId)
                    .OnDelete(DeleteBehavior.Cascade);

                etidade.HasOne(c => c.Usuario)
                    .WithMany(u => u.Comentarios)
                    .HasForeignKey(c => c.UsuarioId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            construtor.Entity<Anexo>(etidade =>
            {
                etidade.ToTable("Anexos");
                etidade.HasKey(a => a.Id);
                etidade.Property(a => a.NomeArquivo).HasMaxLength(250).IsRequired();
                etidade.Property(a => a.CaminhoArquivo).HasMaxLength(500).IsRequired();
                etidade.Property(a => a.TipoConteudo).HasMaxLength(100).IsRequired();

                etidade.HasOne(a => a.InstanciaProcesso)
                    .WithMany(ip => ip.Anexos)
                    .HasForeignKey(a => a.InstanciaProcessoId)
                    .OnDelete(DeleteBehavior.Cascade);

                etidade.HasOne(a => a.TarefaProcesso)
                    .WithMany()
                    .HasForeignKey(a => a.TarefaProcessoId)
                    .OnDelete(DeleteBehavior.Restrict);

                etidade.HasOne(a => a.UsuarioUpload)
                    .WithMany()
                    .HasForeignKey(a => a.UsuarioUploadId)
                    .OnDelete(DeleteBehavior.Restrict);
            });
        }
    }
}
