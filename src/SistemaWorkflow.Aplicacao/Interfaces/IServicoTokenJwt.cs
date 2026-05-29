using System.Collections.Generic;
using SistemaWorkflow.Dominio.Entidades;

namespace SistemaWorkflow.Aplicacao.Interfaces
{
    public interface IServicoTokenJwt
    {
        string GerarToken(Usuario usuario, IEnumerable<string> perfis, IEnumerable<string> permissoes);
    }
}
