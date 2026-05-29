using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SistemaWorkflow.Aplicacao.DTOs;
using SistemaWorkflow.Aplicacao.Interfaces;
using SistemaWorkflow.Infraestrutura.BancoDados;

namespace SistemaWorkflow.Api.Controllers
{
    [ApiController]
    [Route("api/autenticacao")]
    public class AutenticacaoController : ControllerBase
    {
        private readonly ContextoWorkflow _contexto;
        private readonly IServicoCriptografia _servicoCriptografia;
        private readonly IServicoTokenJwt _servicoTokenJwt;

        public AutenticacaoController(
            ContextoWorkflow contexto,
            IServicoCriptografia servicoCriptografia,
            IServicoTokenJwt servicoTokenJwt)
        {
            _contexto = contexto;
            _servicoCriptografia = servicoCriptografia;
            _servicoTokenJwt = servicoTokenJwt;
        }

        [HttpPost("login")]
        public async Task<IActionResult> RealizarLogin([FromBody] RequisicaoLogin requisicao)
        {
            if (requisicao == null || string.IsNullOrWhiteSpace(requisicao.Email) || string.IsNullOrWhiteSpace(requisicao.Senha))
            {
                return BadRequest("O e-mail e a senha devem ser fornecidos.");
            }

            // Busca o usuário e inclui perfis e permissões associadas
            var usuario = await _contexto.Usuarios
                .Include(u => u.UsuarioPerfis)
                    .ThenInclude(up => up.Perfil)
                        .ThenInclude(p => p.PerfilPermissoes)
                            .ThenInclude(pp => pp.Permissao)
                .FirstOrDefaultAsync(u => u.Email == requisicao.Email);

            if (usuario == null)
            {
                return Unauthorized("Credenciais inválidas.");
            }

            // Verifica a senha
            var senhaValida = _servicoCriptografia.VerificarSenha(requisicao.Senha, usuario.SenhaHash);
            if (!senhaValida)
            {
                return Unauthorized("Credenciais inválidas.");
            }

            if (!usuario.Ativo)
            {
                return BadRequest("Usuário está inativo no sistema.");
            }

            // Extrai a lista de nomes dos perfis
            var perfis = usuario.UsuarioPerfis
                .Select(up => up.Perfil.Nome)
                .Distinct()
                .ToList();

            // Extrai a lista de chaves de permissões
            var permissoes = usuario.UsuarioPerfis
                .Select(up => up.Perfil)
                .SelectMany(p => p.PerfilPermissoes)
                .Select(pp => pp.Permissao.Chave)
                .Distinct()
                .ToList();

            // Gera o token JWT
            var token = _servicoTokenJwt.GerarToken(usuario, perfis, permissoes);

            var resposta = new RespostaLogin
            {
                Token = token,
                NomeUsuario = usuario.Nome,
                EmailUsuario = usuario.Email
            };

            return Ok(resposta);
        }
    }
}
