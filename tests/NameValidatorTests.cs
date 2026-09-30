using ExamTicketGenerator;
using Xunit;

namespace ExamTicketGenerator.Tests
{
    public class NameValidatorTests
    {
        [Theory]
        [InlineData("Ivanov", "Ivanov")]
        [InlineData("  Ivanov  ", "Ivanov")]
        [InlineData("  Petrov Sidorov  ", "Petrov Sidorov")]
        public void Normalize_TrimsSurroundingSpaces(string raw, string expected)
        {
            var result = NameValidator.Normalize(raw);

            Assert.Equal(expected, result);
        }

        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        [InlineData(null)]
        public void Normalize_ReturnsNull_ForEmptyOrWhitespaceInput(string? raw)
        {
            var result = NameValidator.Normalize(raw);

            Assert.Null(result);
        }

        [Theory]
        [InlineData("Ivanov")]
        [InlineData("Иванов")]
        [InlineData("Ёлкин")]
        [InlineData("Петров-Водкин")]
        [InlineData("O'Brien")]
        [InlineData("Petrov Sidorov")]
        public void IsValid_ReturnsTrue_ForLettersWithSingleSeparators(string name)
        {
            Assert.True(NameValidator.IsValid(name));
        }

        [Theory]
        [InlineData("Ivanov1")]
        [InlineData("123")]
        [InlineData("Ivan_ov")]
        [InlineData("Ivanov!")]
        [InlineData("@Ivanov")]
        [InlineData("Ivan.ov")]
        [InlineData("-Ivanov")]
        [InlineData("Ivanov-")]
        [InlineData("Petrov--Vodkin")]
        [InlineData("Petrov  Sidorov")]
        [InlineData("\bIvanov")]
        [InlineData("")]
        [InlineData(null)]
        public void IsValid_ReturnsFalse_ForDigitsAndOtherSymbols(string? name)
        {
            Assert.False(NameValidator.IsValid(name));
        }
    }
}
