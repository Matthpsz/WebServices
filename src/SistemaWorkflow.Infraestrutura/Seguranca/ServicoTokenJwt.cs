using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using SistemaWorkflow.Aplicacao.Interfaces;
using SistemaWorkflow.Dominio.Entidades;

namespace SistemaWorkflow.Infraestrutura.Seguranca
{
    public class ServicoTokenJwt : IServicoTokenJwt
    {
        private readonly IConfiguration _configuracao;

        public ServicoTokenJwt(IConfiguration configuracao)
        {
            _configuracao = configuracao;
        }

        public string GerarToken(Usuario usuario, IEnumerable<string> perfis, IEnumerable<string> permissoes)
        {
            if (usuario == null)
                throw new ArgumentNullException(nameof(usuario));

            var chaveSecreta = _configuracao["ConfiguracoesJwt:ChaveSecreta"] 
                ?? throw new InvalidOperationException("Chave secreta do JWT não está configurada no appsettings.");

            var emissor = _configuracao["ConfiguracoesJwt:Emissor"];
            var audiencia = _configuracao["ConfiguracoesJwt:Audiencia"];
            var expiracaoMinutosStr = _configuracao["ConfiguracoesJwt:TempoExpiracaoMinutos"] ?? "120";

            if (!double.TryParse(expiracaoMinutosStr, out var expiracaoMinutos))
            {
                expiracaoMinutos = 120;
            }

            var chaveSeguranca = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(chaveSecreta));
            var credenciaisAssinatura = new SigningCredentials(chaveSeguranca, SecurityAlgorithms.HmacSha256);

            var reivindicacoes = new List<Claim>
            {
                new Claim(JwtRegisteredClaimNames.Sub, usuario.Id.ToString()),
                new Claim(JwtRegisteredClaimNames.Email, usuario.Email),
                new Claim("nome", usuario.Nome),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
            };

            // Adiciona perfis (Roles)
            foreach (var perfil in perfis)
            {
                reivindicacoes.Add(new Claim(ClaimTypes.Role, perfil));
            }

            // Adiciona permissões individuais (Policies/Claims personalizadas)
            foreach (var permissao in permissoes)
            {
                reivindicacoes.Add(new Claim("permissao", permissao));
            }

            var tokenSeguranca = new JwtSecurityToken(
                issuer: emissor,
                audience: audiencia,
                claims: reivindicacoes,
                expires: DateTime.UtcNow.AddMinutes(expiracaoMinutos),
                signingCredentials: credenciaisAssinatura);

            return new JwtSecurityTokenHandler().WriteToken(tokenSeguranca);
        }
    }
}
