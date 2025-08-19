using System.Collections.Generic;

namespace EstagioGO.Models.Domain
{
    public class Justificativa
    {
        public int Id { get; set; }
        public string Descricao { get; set; }
        public bool RequerDocumentacao { get; set; }
        public bool Ativo { get; set; } = true;

        // Relacionamento inverso
        public ICollection<Frequencia> Frequencias { get; set; }
    }
}