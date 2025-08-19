using System;

namespace EstagioGO.Models.Domain
{
    public class Frequencia
    {
        public int Id { get; set; }
        public int EstagiarioId { get; set; }
        public Estagiario Estagiario { get; set; }
        public DateTime Data { get; set; }
        public TimeSpan? HoraEntrada { get; set; }
        public TimeSpan? HoraSaida { get; set; }
        public bool Presente { get; set; }
        public string Observacao { get; set; }
        public int? JustificativaId { get; set; }
        public Justificativa Justificativa { get; set; }
        public DateTime DataRegistro { get; set; } = DateTime.Now;
        public string RegistradoPorId { get; set; }
        public ApplicationUser RegistradoPor { get; set; }
    }
}