using System;
using System.Collections.Generic;

namespace EstagioGO.Models.Domain
{
    public class Estagiario
    {
        public int Id { get; set; }
        public string Nome { get; set; }
        public string Matricula { get; set; }
        public string Curso { get; set; }
        public string InstituicaoEnsino { get; set; }
        public DateTime DataInicio { get; set; }
        public DateTime? DataTermino { get; set; }
        public string SupervisorId { get; set; }
        public ApplicationUser Supervisor { get; set; }
        public string CoordenadorId { get; set; }
        public ApplicationUser Coordenador { get; set; }
        public string UserId { get; set; }
        public ApplicationUser User { get; set; }
        public bool Ativo { get; set; } = true;
        public DateTime DataCadastro { get; set; } = DateTime.Now;

        // Relacionamentos
        public ICollection<Frequencia> Frequencias { get; set; }
        public ICollection<Avaliacao> Avaliacoes { get; set; }
    }
}