using System;
using System.Linq;
using SistemaWorkflow.Aplicacao.Interfaces;
using SistemaWorkflow.Dominio.Entidades;

namespace SistemaWorkflow.Infraestrutura.BancoDados
{
    public class SemeadorBancoDados
    {
        public static void Semear(ContextoWorkflow contexto, IServicoCriptografia servicoCriptografia)
        {
            if (contexto == null)
                throw new ArgumentNullException(nameof(contexto));
            if (servicoCriptografia == null)
                throw new ArgumentNullException(nameof(servicoCriptografia));

            // 1. Permissões
            var permissoesDefinidas = new[]
            {
                new Permissao { Nome = "Configurações Gerais do Sistema", Chave = "Configuracao.Geral", Descricao = "Permite alterar configurações globais do sistema" },
                new Permissao { Nome = "Criar Processos", Chave = "Processo.Criar", Descricao = "Permite criar novas definições de processos de workflow" },
                new Permissao { Nome = "Editar Processos", Chave = "Processo.Editar", Descricao = "Permite alterar e versionar as definições de processos" },
                new Permissao { Nome = "Excluir Processos", Chave = "Processo.Excluir", Descricao = "Permite excluir definições de processos do sistema" },
                new Permissao { Nome = "Validar/Homologar Processos", Chave = "Processo.Validar", Descricao = "Permite aprovar e publicar processos criados por chefes de equipe" },
                new Permissao { Nome = "Aprovar Tarefas", Chave = "Tarefa.Aprovar", Descricao = "Permite aprovar, rejeitar ou retornar tarefas ativas" },
                new Permissao { Nome = "Executar Tarefas", Chave = "Tarefa.Executar", Descricao = "Permite preencher formulários dinâmicos e concluir tarefas" },
                new Permissao { Nome = "Visualizar Tarefas", Chave = "Tarefa.Visualizar", Descricao = "Permite acompanhar o andamento de instâncias e tarefas" },
                new Permissao { Nome = "Gerenciar Equipes", Chave = "Equipe.Gerenciar", Descricao = "Permite criar, editar e vincular usuários a equipes" },
                new Permissao { Nome = "Gerenciar Usuários", Chave = "Usuario.Gerenciar", Descricao = "Permite gerenciar usuários, perfis e permissões" }
            };

            foreach (var permissao in permissoesDefinidas)
            {
                if (!contexto.Permissoes.Any(p => p.Chave == permissao.Chave))
                {
                    contexto.Permissoes.Add(permissao);
                }
            }
            contexto.SaveChanges();

            // 2. Perfis
            var perfilAdmin = contexto.Perfis.FirstOrDefault(p => p.Nome == "Administrador");
            if (perfilAdmin == null)
            {
                perfilAdmin = new Perfil { Nome = "Administrador", Descricao = "Acesso total a todas as funcionalidades do sistema e homologação de processos" };
                contexto.Perfis.Add(perfilAdmin);
                contexto.SaveChanges();
            }

            var perfilGestor = contexto.Perfis.FirstOrDefault(p => p.Nome == "Gestor");
            if (perfilGestor == null)
            {
                perfilGestor = new Perfil { Nome = "Gestor", Descricao = "Gestão de processos, acompanhamento de SLAs e gerenciar equipes" };
                contexto.Perfis.Add(perfilGestor);
                contexto.SaveChanges();
            }

            var perfilColaborador = contexto.Perfis.FirstOrDefault(p => p.Nome == "Colaborador");
            if (perfilColaborador == null)
            {
                perfilColaborador = new Perfil { Nome = "Colaborador", Descricao = "Execução de tarefas sob sua responsabilidade e início de processos" };
                contexto.Perfis.Add(perfilColaborador);
                contexto.SaveChanges();
            }

            // 3. Associação de Permissões aos Perfis
            var todasPermissoes = contexto.Permissoes.ToList();

            // Perfil Administrador: ganha todas as permissões
            foreach (var permissao in todasPermissoes)
            {
                if (!contexto.PerfilPermissoes.Any(pp => pp.PerfilId == perfilAdmin.Id && pp.PermissaoId == permissao.Id))
                {
                    contexto.PerfilPermissoes.Add(new PerfilPermissao
                    {
                        PerfilId = perfilAdmin.Id,
                        PermissaoId = permissao.Id
                    });
                }
            }

            // Perfil Gestor: ganha criação de processos, tarefas e equipes
            var chavesGestor = new[] { "Processo.Criar", "Processo.Editar", "Tarefa.Aprovar", "Tarefa.Executar", "Tarefa.Visualizar", "Equipe.Gerenciar" };
            foreach (var permissao in todasPermissoes.Where(p => chavesGestor.Contains(p.Chave)))
            {
                if (!contexto.PerfilPermissoes.Any(pp => pp.PerfilId == perfilGestor.Id && pp.PermissaoId == permissao.Id))
                {
                    contexto.PerfilPermissoes.Add(new PerfilPermissao
                    {
                        PerfilId = perfilGestor.Id,
                        PermissaoId = permissao.Id
                    });
                }
            }

            // Perfil Colaborador: ganha apenas execução e visualização de tarefas
            var chavesColaborador = new[] { "Tarefa.Executar", "Tarefa.Visualizar" };
            foreach (var permissao in todasPermissoes.Where(p => chavesColaborador.Contains(p.Chave)))
            {
                if (!contexto.PerfilPermissoes.Any(pp => pp.PerfilId == perfilColaborador.Id && pp.PermissaoId == permissao.Id))
                {
                    contexto.PerfilPermissoes.Add(new PerfilPermissao
                    {
                        PerfilId = perfilColaborador.Id,
                        PermissaoId = permissao.Id
                    });
                }
            }
            contexto.SaveChanges();

            // 4. Usuário Administrador Padrão
            var emailAdmin = "admin@workflow.com.br";
            var usuarioAdmin = contexto.Usuarios.FirstOrDefault(u => u.Email == emailAdmin);
            if (usuarioAdmin == null)
            {
                usuarioAdmin = new Usuario
                {
                    Nome = "Administrador do Sistema",
                    Email = emailAdmin,
                    SenhaHash = servicoCriptografia.CriptografarSenha("Admin@1234"), // Senha padrão recomendada
                    Ativo = true
                };
                contexto.Usuarios.Add(usuarioAdmin);
                contexto.SaveChanges();

                // Vincula ao perfil de Administrador
                contexto.UsuarioPerfis.Add(new UsuarioPerfil
                {
                    UsuarioId = usuarioAdmin.Id,
                    PerfilId = perfilAdmin.Id
                });
                contexto.SaveChanges();
            }

            // 5. SEED ADICIONAL PARA TESTES OPERACIONAIS (Usuários, Equipes, Processos e Tarefas Ativas)
            
            // A. Usuários de Teste
            var matheus = contexto.Usuarios.FirstOrDefault(u => u.Email == "matheus@workflow.com.br");
            if (matheus == null)
            {
                matheus = new Usuario { Nome = "Matheus Silva", Email = "matheus@workflow.com.br", SenhaHash = servicoCriptografia.CriptografarSenha("Admin@1234"), Ativo = true };
                contexto.Usuarios.Add(matheus);
                contexto.SaveChanges();
                contexto.UsuarioPerfis.Add(new UsuarioPerfil { UsuarioId = matheus.Id, PerfilId = perfilAdmin.Id });
                contexto.SaveChanges();
            }

            var ana = contexto.Usuarios.FirstOrDefault(u => u.Email == "ana.souza@workflow.com.br");
            if (ana == null)
            {
                ana = new Usuario { Nome = "Ana Souza", Email = "ana.souza@workflow.com.br", SenhaHash = servicoCriptografia.CriptografarSenha("Admin@1234"), Ativo = true };
                contexto.Usuarios.Add(ana);
                contexto.SaveChanges();
                contexto.UsuarioPerfis.Add(new UsuarioPerfil { UsuarioId = ana.Id, PerfilId = perfilGestor.Id });
                contexto.SaveChanges();
            }

            var lucas = contexto.Usuarios.FirstOrDefault(u => u.Email == "lucas.lima@workflow.com.br");
            if (lucas == null)
            {
                lucas = new Usuario { Nome = "Lucas Lima", Email = "lucas.lima@workflow.com.br", SenhaHash = servicoCriptografia.CriptografarSenha("Admin@1234"), Ativo = true };
                contexto.Usuarios.Add(lucas);
                contexto.SaveChanges();
                contexto.UsuarioPerfis.Add(new UsuarioPerfil { UsuarioId = lucas.Id, PerfilId = perfilColaborador.Id });
                contexto.SaveChanges();
            }

            // B. Equipes/Setores
            var equipeTI = contexto.Equipes.FirstOrDefault(e => e.Nome == "Infraestrutura de TI");
            if (equipeTI == null)
            {
                equipeTI = new Equipe { Nome = "Infraestrutura de TI", Descricao = "Suporte de redes, servidores, telecom e hardware corporativo." };
                contexto.Equipes.Add(equipeTI);
                contexto.SaveChanges();

                contexto.EquipeUsuarios.Add(new EquipeUsuario { EquipeId = equipeTI.Id, UsuarioId = matheus.Id, AdministradorEquipe = true, Ativo = true, DataCriacao = DateTime.Now, DataAtualizacao = DateTime.Now });
                contexto.EquipeUsuarios.Add(new EquipeUsuario { EquipeId = equipeTI.Id, UsuarioId = lucas.Id, AdministradorEquipe = false, Ativo = true, DataCriacao = DateTime.Now, DataAtualizacao = DateTime.Now });
                contexto.SaveChanges();
            }

            var equipeRH = contexto.Equipes.FirstOrDefault(e => e.Nome == "Recursos Humanos");
            if (equipeRH == null)
            {
                equipeRH = new Equipe { Nome = "Recursos Humanos", Descricao = "Atração, admissão, onboardings e gestão do clima organizacional." };
                contexto.Equipes.Add(equipeRH);
                contexto.SaveChanges();

                contexto.EquipeUsuarios.Add(new EquipeUsuario { EquipeId = equipeRH.Id, UsuarioId = ana.Id, AdministradorEquipe = true });
                contexto.EquipeUsuarios.Add(new EquipeUsuario { EquipeId = equipeRH.Id, UsuarioId = usuarioAdmin.Id, AdministradorEquipe = false });
                contexto.SaveChanges();
            }

            // C. Processo 1: Manutenção de Notebooks (TI)
            var processoManut = contexto.Processos.FirstOrDefault(p => p.Nome == "Manutenção de Notebooks");
            if (processoManut == null)
            {
                processoManut = new Processo { Nome = "Manutenção de Notebooks", Descricao = "Mapeamento operacional para manutenção física e lógica de notebooks corporativos.", EquipeId = equipeTI.Id };
                contexto.Processos.Add(processoManut);
                contexto.SaveChanges();

                var versaoManut = new VersaoProcesso
                {
                    ProcessoId = processoManut.Id,
                    NumeroVersao = 1,
                    Status = Dominio.Enums.StatusVersaoProcesso.Ativo,
                    UsuarioValidadorId = usuarioAdmin.Id,
                    DataValidacao = DateTime.UtcNow,
                    JustificativaValidacao = "Homologado automaticamente na semeadura para testes."
                };
                contexto.VersoesProcesso.Add(versaoManut);
                contexto.SaveChanges();

                var etapaTriagem = new Atividade { VersaoProcessoId = versaoManut.Id, Nome = "Triagem e Diagnóstico", Descricao = "Verificar estado do hardware e diagnosticar se o problema é lógico ou físico.", SlaMinutos = 60, Ordem = 1, EhInicial = true, ResponsavelEquipeId = equipeTI.Id };
                var etapaReparo = new Atividade { VersaoProcessoId = versaoManut.Id, Nome = "Reparo Técnico", Descricao = "Proceder com a troca de componentes defeituosos ou restauração do sistema.", SlaMinutos = 240, Ordem = 2, ResponsavelEquipeId = equipeTI.Id };
                var etapaEntrega = new Atividade { VersaoProcessoId = versaoManut.Id, Nome = "Encerramento e Entrega", Descricao = "Higienizar notebook, validar com usuário e arquivar chamado.", SlaMinutos = 30, Ordem = 3, EhFinal = true, ResponsavelEquipeId = equipeTI.Id };

                contexto.Atividades.AddRange(etapaTriagem, etapaReparo, etapaEntrega);
                contexto.SaveChanges();

                contexto.TransicoesAtividade.Add(new TransicaoAtividade { VersaoProcessoId = versaoManut.Id, AtividadeOrigemId = etapaTriagem.Id, AtividadeDestinoId = etapaReparo.Id, Nome = "Encaminhar para Reparo" });
                contexto.TransicoesAtividade.Add(new TransicaoAtividade { VersaoProcessoId = versaoManut.Id, AtividadeOrigemId = etapaTriagem.Id, AtividadeDestinoId = etapaEntrega.Id, Nome = "Sem Defeito / Resolver" });
                contexto.TransicoesAtividade.Add(new TransicaoAtividade { VersaoProcessoId = versaoManut.Id, AtividadeOrigemId = etapaReparo.Id, AtividadeDestinoId = etapaEntrega.Id, Nome = "Concluir Manutenção" });
                contexto.SaveChanges();

                // Instancia e Tarefa Ativa para Matheus
                var instanciaManut = new InstanciaProcesso { VersaoProcessoId = versaoManut.Id, CodigoIdentificador = "WF-MANUT-001", Status = Dominio.Enums.StatusInstanciaProcesso.Ativa, UsuarioCriadorId = matheus.Id, DataInicio = DateTime.UtcNow };
                contexto.InstanciasProcesso.Add(instanciaManut);
                contexto.SaveChanges();

                contexto.TarefasProcesso.Add(new TarefaProcesso { InstanciaProcessoId = instanciaManut.Id, AtividadeId = etapaTriagem.Id, Status = Dominio.Enums.StatusTarefaProcesso.Pendente, EquipeResponsavelId = equipeTI.Id, DataLimite = DateTime.UtcNow.AddMinutes(60) });
                contexto.SaveChanges();
            }

            // D. Processo 2: Admissão de Novo Colaborador (RH)
            var processoAdmis = contexto.Processos.FirstOrDefault(p => p.Nome == "Admissão de Novo Colaborador");
            if (processoAdmis == null)
            {
                processoAdmis = new Processo { Nome = "Admissão de Novo Colaborador", Descricao = "Mapeamento operacional para onboarding, entrega de acessos e contratação formal.", EquipeId = equipeRH.Id };
                contexto.Processos.Add(processoAdmis);
                contexto.SaveChanges();

                var versaoAdmis = new VersaoProcesso
                {
                    ProcessoId = processoAdmis.Id,
                    NumeroVersao = 1,
                    Status = Dominio.Enums.StatusVersaoProcesso.Ativo,
                    UsuarioValidadorId = usuarioAdmin.Id,
                    DataValidacao = DateTime.UtcNow,
                    JustificativaValidacao = "Homologado automaticamente na semeadura para testes."
                };
                contexto.VersoesProcesso.Add(versaoAdmis);
                contexto.SaveChanges();

                var etapaRHDoc = new Atividade { VersaoProcessoId = versaoAdmis.Id, Nome = "Coleta de Documentos", Descricao = "Efetuar recepção dos documentos admissionais do candidato.", SlaMinutos = 120, Ordem = 1, EhInicial = true, ResponsavelEquipeId = equipeRH.Id };
                var etapaTIDeploy = new Atividade { VersaoProcessoId = versaoAdmis.Id, Nome = "Configuração de Contas e Acessos", Descricao = "Criar e-mail, configurar acessos no Active Directory e disponibilizar notebook.", SlaMinutos = 180, Ordem = 2, ResponsavelEquipeId = equipeTI.Id };
                var etapaRHOnboard = new Atividade { VersaoProcessoId = versaoAdmis.Id, Nome = "Integração e Onboarding", Descricao = "Realizar integração institucional com o colaborador na empresa.", SlaMinutos = 60, Ordem = 3, EhFinal = true, ResponsavelEquipeId = equipeRH.Id };

                contexto.Atividades.AddRange(etapaRHDoc, etapaTIDeploy, etapaRHOnboard);
                contexto.SaveChanges();

                contexto.TransicoesAtividade.Add(new TransicaoAtividade { VersaoProcessoId = versaoAdmis.Id, AtividadeOrigemId = etapaRHDoc.Id, AtividadeDestinoId = etapaTIDeploy.Id, Nome = "Enviar para TI" });
                contexto.TransicoesAtividade.Add(new TransicaoAtividade { VersaoProcessoId = versaoAdmis.Id, AtividadeOrigemId = etapaTIDeploy.Id, AtividadeDestinoId = etapaRHOnboard.Id, Nome = "Acessos Criados" });
                contexto.SaveChanges();

                // Instancia e Tarefa Ativa para Ana Souza
                var instanciaAdmis = new InstanciaProcesso { VersaoProcessoId = versaoAdmis.Id, CodigoIdentificador = "WF-ADMIS-001", Status = Dominio.Enums.StatusInstanciaProcesso.Ativa, UsuarioCriadorId = ana.Id, DataInicio = DateTime.UtcNow };
                contexto.InstanciasProcesso.Add(instanciaAdmis);
                contexto.SaveChanges();

                // Esta tarefa fica pendente especificamente para a equipe de TI (Configuração de Acessos)!
                contexto.TarefasProcesso.Add(new TarefaProcesso { InstanciaProcessoId = instanciaAdmis.Id, AtividadeId = etapaTIDeploy.Id, Status = Dominio.Enums.StatusTarefaProcesso.Pendente, EquipeResponsavelId = equipeTI.Id, DataLimite = DateTime.UtcNow.AddMinutes(180) });
                contexto.SaveChanges();
            }

            // Correção Dinâmica de Equipe e Liderança (Garante Matheus = TI, Ana = RH no banco já existente)
            var uMatheus = contexto.Usuarios.FirstOrDefault(u => u.Email == "matheus@workflow.com.br");
            var uAna = contexto.Usuarios.FirstOrDefault(u => u.Email == "ana.souza@workflow.com.br");
            var eqTI = contexto.Equipes.FirstOrDefault(e => e.Nome == "Infraestrutura de TI");
            var eqRH = contexto.Equipes.FirstOrDefault(e => e.Nome == "Recursos Humanos");

            if (eqTI != null && eqRH != null && uMatheus != null && uAna != null)
            {
                // Ana deve liderar APENAS o RH. Remove a liderança da Ana na TI se houver.
                var anaVinculoTI = contexto.EquipeUsuarios.FirstOrDefault(eu => eu.EquipeId == eqTI.Id && eu.UsuarioId == uAna.Id);
                if (anaVinculoTI != null)
                {
                    contexto.EquipeUsuarios.Remove(anaVinculoTI);
                }

                // Matheus deve liderar a TI.
                var matheusVinculoTI = contexto.EquipeUsuarios.FirstOrDefault(eu => eu.EquipeId == eqTI.Id && eu.UsuarioId == uMatheus.Id);
                if (matheusVinculoTI == null)
                {
                    contexto.EquipeUsuarios.Add(new EquipeUsuario 
                    { 
                        EquipeId = eqTI.Id, 
                        UsuarioId = uMatheus.Id, 
                        AdministradorEquipe = true,
                        Ativo = true,
                        DataCriacao = DateTime.Now,
                        DataAtualizacao = DateTime.Now
                    });
                }
                else if (!matheusVinculoTI.AdministradorEquipe)
                {
                    matheusVinculoTI.AdministradorEquipe = true;
                    contexto.EquipeUsuarios.Update(matheusVinculoTI);
                }

                // Ana deve liderar o RH.
                var anaVinculoRH = contexto.EquipeUsuarios.FirstOrDefault(eu => eu.EquipeId == eqRH.Id && eu.UsuarioId == uAna.Id);
                if (anaVinculoRH == null)
                {
                    contexto.EquipeUsuarios.Add(new EquipeUsuario 
                    { 
                        EquipeId = eqRH.Id, 
                        UsuarioId = uAna.Id, 
                        AdministradorEquipe = true,
                        Ativo = true,
                        DataCriacao = DateTime.Now,
                        DataAtualizacao = DateTime.Now
                    });
                }
                else if (!anaVinculoRH.AdministradorEquipe)
                {
                    anaVinculoRH.AdministradorEquipe = true;
                    contexto.EquipeUsuarios.Update(anaVinculoRH);
                }

                contexto.SaveChanges();
            }
        }
    }
}
