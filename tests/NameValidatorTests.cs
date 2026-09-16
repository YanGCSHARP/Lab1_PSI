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
    }
}
