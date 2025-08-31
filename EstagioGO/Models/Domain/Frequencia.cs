using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace EstagioGO.Models.Domain
{
    public class Frequencia : IValidatableObject
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "O estagiário é obrigatório")]
        public int EstagiarioId { get; set; }

        [ForeignKey("EstagiarioId")]
        [ValidateNever] // Adicionar esta linha
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

        [AllowNull]
        [StringLength(500, ErrorMessage = "A observação não pode ter mais de 500 caracteres")]
        public string? Observacao { get; set; }

        [Display(Name = "Data de Registro")]
        public DateTime DataRegistro { get; set; } = DateTime.Now;

        [AllowNull]
        [Display(Name = "Motivo")]
        public string Motivo { get; set; }

        [AllowNull]
        [Display(Name = "Detalhamento")]
        [StringLength(500, ErrorMessage = "O detalhamento não pode ter mais de 500 caracteres")]
        public string Detalhamento { get; set; }

        [Required(ErrorMessage = "O usuário que registrou é obrigatório")]
        public string RegistradoPorId { get; set; }

        [ForeignKey("RegistradoPorId")]
        [ValidateNever] // Adicionar esta linha
        public ApplicationUser RegistradoPor { get; set; }

        // Validação condicional: Se presente == false, justificativa obrigatória
        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (Presente == false && Motivo == null)
            {
                yield return new ValidationResult(
                    "Justificativa é obrigatória quando o estagiário está ausente.",
                    new[] { nameof(Motivo) });
            }
        }
    }
}