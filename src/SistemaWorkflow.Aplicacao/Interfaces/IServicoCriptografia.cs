namespace SistemaWorkflow.Aplicacao.Interfaces
{
    public interface IServicoCriptografia
    {
        string CriptografarSenha(string senha);
        bool VerificarSenha(string senha, string senhaHash);
    }
}
