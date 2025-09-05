using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;

namespace EstagioGO.Models.Domain
{
    public class CPFAttribute : ValidationAttribute
    {
        protected override ValidationResult IsValid(object value, ValidationContext validationContext)
        {
            if (value == null || string.IsNullOrEmpty(value.ToString()))
                return ValidationResult.Success;

            var cpf = value.ToString();

            // Remove formatação
            cpf = Regex.Replace(cpf, @"[^\d]", "");

            // Verifica se tem 11 dígitos
            if (cpf.Length != 11)
                return new ValidationResult("CPF deve conter 11 dígitos.");

            // Verifica se todos os dígitos são iguais
            if (new string(cpf[0], 11) == cpf)
                return new ValidationResult("CPF inválido.");

            // Calcula o primeiro dígito verificador
            int[] multiplicador1 = [10, 9, 8, 7, 6, 5, 4, 3, 2];
            int soma = 0;
            for (int i = 0; i < 9; i++)
                soma += int.Parse(cpf[i].ToString()) * multiplicador1[i];
            int resto = soma % 11;
            int digitoVerificador1 = resto < 2 ? 0 : 11 - resto;

            // Calcula o segundo dígito verificador
            int[] multiplicador2 = [11, 10, 9, 8, 7, 6, 5, 4, 3, 2];
            soma = 0;
            for (int i = 0; i < 10; i++)
                soma += int.Parse(cpf[i].ToString()) * multiplicador2[i];
            resto = soma % 11;
            int digitoVerificador2 = resto < 2 ? 0 : 11 - resto;

            // Verifica se os dígitos calculados batem com os informados
            if (int.Parse(cpf[9].ToString()) == digitoVerificador1 && int.Parse(cpf[10].ToString()) == digitoVerificador2)
                return ValidationResult.Success;

            return new ValidationResult("CPF inválido.");
        }
    }
}