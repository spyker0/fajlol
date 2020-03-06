namespace TCC.Models
{
    public class Campeao
    {
        public int ID { get; set; }
        public string Nome { get; set; }
        public int PartidasGanhas { get; set; }
        public int PartidasPerdidas { get; set; }
        public decimal TaxaVitoria { get; set; }

        public int PartidasTotais
        {
            get
            {
                return PartidasGanhas + PartidasPerdidas;
            }
        }
    }
}