namespace EstagioGO.Models.Domain
{
    public class ItemAvaliacao
    {
        public int Id { get; set; }
        public int AvaliacaoId { get; set; }
        public Avaliacao Avaliacao { get; set; }
        public string Descricao { get; set; }
        public int Nota { get; set; }
    }
}