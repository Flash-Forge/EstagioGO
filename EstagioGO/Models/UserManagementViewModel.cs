using System;

namespace EstagioGO.Models
{
    public class UserManagementViewModel
    {
        public string Id { get; set; }
        public string NomeCompleto { get; set; }
        public string Email { get; set; }
        public string Cargo { get; set; }
        public string Role { get; set; }
        public DateTime DataCadastro { get; set; }
        public bool Ativo { get; set; }
        public bool PrimeiroAcessoConcluido { get; set; }
    }
}