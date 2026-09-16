using System;
using ExamTicketGenerator;
using Xunit;

namespace ExamTicketGenerator.Tests
{
    public class TicketNumberGeneratorTests
    {
        [Fact]
        public void Generate_AlwaysReturnsValueWithinRange()
        {
            var random = new Random(12345);

            for (var i = 0; i < 1000; i++)
            {
                var ticket = TicketNumberGenerator.Generate(random);

                Assert.InRange(ticket, TicketNumberGenerator.MinTicket, TicketNumberGenerator.MaxTicket);
            }
        }
    }
}