using System;
using System.Security.Cryptography;
using SistemaWorkflow.Aplicacao.Interfaces;

namespace SistemaWorkflow.Infraestrutura.Seguranca
{
    public class ServicoCriptografia : IServicoCriptografia
    {
        private const int TamanhoSal = 16; // 128 bits
        private const int TamanhoHash = 32; // 256 bits
        private const int Iteracoes = 10000;

        public string CriptografarSenha(string senha)
        {
            if (string.IsNullOrWhiteSpace(senha))
                throw new ArgumentException("A senha não pode ser vazia.", nameof(senha));

            byte[] sal;
            byte[] hash;
            
            using (var derivador = new Rfc2898DeriveBytes(senha, TamanhoSal, Iteracoes, HashAlgorithmName.SHA256))
            {
                sal = derivador.Salt;
                hash = derivador.GetBytes(TamanhoHash);
            }

            var bytesResultado = new byte[TamanhoSal + TamanhoHash];
            Array.Copy(sal, 0, bytesResultado, 0, TamanhoSal);
            Array.Copy(hash, 0, bytesResultado, TamanhoSal, TamanhoHash);

            return Convert.ToBase64String(bytesResultado);
        }

        public bool VerificarSenha(string senha, string senhaHash)
        {
            if (string.IsNullOrWhiteSpace(senha) || string.IsNullOrWhiteSpace(senhaHash))
                return false;

            try
            {
                var bytesResultado = Convert.FromBase64String(senhaHash);
                if (bytesResultado.Length != TamanhoSal + TamanhoHash)
                    return false;

                var sal = new byte[TamanhoSal];
                var hashEsperado = new byte[TamanhoHash];

                Array.Copy(bytesResultado, 0, sal, 0, TamanhoSal);
                Array.Copy(bytesResultado, TamanhoSal, hashEsperado, 0, TamanhoHash);

                byte[] hashObtido;
                using (var derivador = new Rfc2898DeriveBytes(senha, sal, Iteracoes, HashAlgorithmName.SHA256))
                {
                    hashObtido = derivador.GetBytes(TamanhoHash);
                }

                // Comparação em tempo constante para evitar ataques de timing
                var diferenca = 0;
                for (var i = 0; i < TamanhoHash; i++)
                {
                    diferenca |= hashObtido[i] ^ hashEsperado[i];
                }

                return diferenca == 0;
            }
            catch
            {
                return false;
            }
        }
    }
}
