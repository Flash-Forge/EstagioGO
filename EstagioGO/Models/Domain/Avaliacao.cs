using System;

namespace EstagioGO.Models.Domain
{
    public class Avaliacao
    {
        public int Id { get; set; }
        public int EstagiarioId { get; set; }
        public Estagiario Estagiario { get; set; }
        public string AvaliadorId { get; set; }
        public ApplicationUser Avaliador { get; set; }
        public DateTime DataAvaliacao { get; set; } = DateTime.Now;
        public int Nota { get; set; }
        public string Comentarios { get; set; }

        // Relacionamentos com itens de avaliação
        public ICollection<ItemAvaliacao> ItensAvaliacao { get; set; }
    }
}