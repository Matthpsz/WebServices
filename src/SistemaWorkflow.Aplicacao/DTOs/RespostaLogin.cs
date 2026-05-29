namespace SistemaWorkflow.Aplicacao.DTOs
{
    public class RespostaLogin
    {
        public string Token { get; set; } = string.Empty;
        public string NomeUsuario { get; set; } = string.Empty;
        public string EmailUsuario { get; set; } = string.Empty;
    }
}
