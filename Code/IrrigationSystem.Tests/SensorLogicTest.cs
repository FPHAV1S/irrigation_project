using Xunit;
using Moq;
using IrrigationSystem.Web.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace IrrigationSystem.Tests
{
    public class SensorDataServiceTests
    {
        [Theory]
        [InlineData(1500, 0)]   // Dry limit
        [InlineData(3800, 100)]  // Wet limit
        [InlineData(2650, 50)]   // Mid point
        [InlineData(1400, 0)]   // Below dry limit
        [InlineData(3900, 100)] // Above wet limit
        [InlineData(0, 0)]      // Very low ADC
        [InlineData(5000, 100)] // Very high ADC
        [InlineData(2000, 21.74)] // Low moisture
        [InlineData(3000, 65.22)] // High moisture
        [InlineData(2200, 30.43)] // Another mid value
        [InlineData(3400, 82.61)] // Higher moisture
        public void CalculateMoisturePercentage_ShouldReturnExpectedValue(int adcValue, double expected)
        {
            // Arrange
            var service = new SensorDataService("", NullLogger<SensorDataService>.Instance);

            // Act
            var result = service.CalculateMoisturePercentage(adcValue);

            // Assert
            Assert.Equal(expected, result, 1);
        }
    }
}