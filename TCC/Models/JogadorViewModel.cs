using RiotSharp.Misc;

namespace TCC.Models
{
    public class JogadorViewModel
    {
        public JogadorViewModel(string mensagemErro)
        {
            if (!string.IsNullOrWhiteSpace(mensagemErro))
            {
                MensagemErro = mensagemErro;
            }
        }

        public string Jogador { get; set; }
        public string Regiao { get; set; }

        public Region Regioes { get; set; }

        public string MensagemErro { get; set; }
    }
}
