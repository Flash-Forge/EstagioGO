using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EstagioGO.Models.Domain
{
    public class Justificativa
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "O motivo é obrigatório")]
        [Display(Name = "Motivo")]
        [StringLength(100, ErrorMessage = "O motivo não pode ter mais de 100 caracteres")]
        public string Motivo { get; set; }

        [Required(ErrorMessage = "O detalhamento é obrigatório")]
        [Display(Name = "Detalhamento")]
        [StringLength(500, ErrorMessage = "O detalhamento não pode ter mais de 500 caracteres")]
        public string Detalhamento { get; set; }

        [Display(Name = "Data de Registro")]
        public DateTime DataRegistro { get; set; } = DateTime.Now;

        [Required]
        [Display(Name = "Registrado por")]
        public string UsuarioRegistroId { get; set; }

        [ForeignKey("UsuarioRegistroId")]
        public ApplicationUser UsuarioRegistro { get; set; }

        public bool Ativo { get; set; } = true;

        // Relacionamento inverso
        public ICollection<Frequencia> Frequencias { get; set; } = [];
    }
}