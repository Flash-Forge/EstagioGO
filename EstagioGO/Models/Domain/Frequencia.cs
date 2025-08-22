using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EstagioGO.Models.Domain
{
    public class Frequencia
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "O estagiário é obrigatório")]
        public int EstagiarioId { get; set; }

        [ForeignKey("EstagiarioId")]
        public Estagiario Estagiario { get; set; }

        [Required(ErrorMessage = "A data é obrigatória")]
        [DataType(DataType.Date)]
        public DateTime Data { get; set; }

        [DataType(DataType.Time)]
        public TimeSpan? HoraEntrada { get; set; }

        [DataType(DataType.Time)]
        public TimeSpan? HoraSaida { get; set; }

        [Required(ErrorMessage = "O campo Presente é obrigatório")]
        public bool Presente { get; set; }

        [StringLength(500, ErrorMessage = "A observação não pode ter mais de 500 caracteres")]
        public string Observacao { get; set; }

        public int? JustificativaId { get; set; }

        [ForeignKey("JustificativaId")]
        public Justificativa Justificativa { get; set; }

        [Display(Name = "Data de Registro")]
        public DateTime DataRegistro { get; set; } = DateTime.Now;

        [Required(ErrorMessage = "O usuário que registrou é obrigatório")]
        public string RegistradoPorId { get; set; }

        [ForeignKey("RegistradoPorId")]
        public ApplicationUser RegistradoPor { get; set; }

        // Validação adicional para garantir que se Presente=false, haja uma justificativa
        public bool IsValid => Presente || (!Presente && JustificativaId.HasValue);
    }
}