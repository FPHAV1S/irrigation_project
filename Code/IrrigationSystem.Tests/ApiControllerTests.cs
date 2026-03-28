using Xunit;
using Moq;
using Microsoft.AspNetCore.Mvc;
using IrrigationSystem.Web.Controllers;
using IrrigationSystem.Web.Services;
using IrrigationSystem.Web.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace IrrigationSystem.Tests
{
    public class ApiControllerTests
    {
        private readonly Mock<ISensorDataService> _mockDataService;
        private readonly Mock<IAdaptiveWateringService> _mockAdaptiveService;
        private readonly ApiController _controller;

        public ApiControllerTests()
        {
            _mockDataService = new Mock<ISensorDataService>();
            _mockAdaptiveService = new Mock<IAdaptiveWateringService>();
            _controller = new ApiController(
                _mockDataService.Object,
                _mockAdaptiveService.Object,
                NullLogger<ApiController>.Instance
            );
        }

        [Fact]
        public async Task GetZones_ReturnsOkResult_WithZones()
        {
            // Arrange
            var expectedZones = new List<Zone>
            {
                new Zone { Id = 1, Name = "Zone 1", PlantType = "Tomato", MoistureThreshold = 30.0f, IsActive = true },
                new Zone { Id = 2, Name = "Zone 2", PlantType = "Lettuce", MoistureThreshold = 40.0f, IsActive = true }
            };
            _mockDataService.Setup(s => s.GetZonesAsync()).ReturnsAsync(expectedZones);

            // Act
            var result = await _controller.GetZones();

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var zones = Assert.IsType<List<Zone>>(okResult.Value);
            Assert.Equal(2, zones.Count);
            Assert.Equal("Zone 1", zones[0].Name);
        }

        [Fact]
        public async Task GetLatestReadings_ReturnsOkResult_WithReadings()
        {
            // Arrange
            var expectedReadings = new List<SensorReading>
            {
                new SensorReading { Id = 1, ZoneId = 1, Moisture = 45.5f, Temperature = 22.0f, Humidity = 60.0f, RecordedAt = DateTime.Now },
                new SensorReading { Id = 2, ZoneId = 2, Moisture = 50.0f, Temperature = 23.0f, Humidity = 65.0f, RecordedAt = DateTime.Now }
            };
            _mockDataService.Setup(s => s.GetLatestReadingsAsync()).ReturnsAsync(expectedReadings);

            // Act
            var result = await _controller.GetLatestReadings();

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var readings = Assert.IsType<List<SensorReading>>(okResult.Value);
            Assert.Equal(2, readings.Count);
            Assert.Equal(45.5f, readings[0].Moisture);
        }

        [Fact]
        public async Task GetZoneHistory_ReturnsOkResult_WithHistory()
        {
            // Arrange
            var zoneId = 1;
            var hours = 48;
            var expectedHistory = new List<SensorReading>
            {
                new SensorReading { Id = 1, ZoneId = 1, Moisture = 40.0f, Temperature = 20.0f, Humidity = 55.0f, RecordedAt = DateTime.Now.AddHours(-1) },
                new SensorReading { Id = 2, ZoneId = 1, Moisture = 45.0f, Temperature = 21.0f, Humidity = 58.0f, RecordedAt = DateTime.Now.AddHours(-2) }
            };
            _mockDataService.Setup(s => s.GetZoneHistoryAsync(zoneId, hours)).ReturnsAsync(expectedHistory);

            // Act
            var result = await _controller.GetZoneHistory(zoneId, hours);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var history = Assert.IsType<List<SensorReading>>(okResult.Value);
            Assert.Equal(2, history.Count);
            Assert.Equal(1, history[0].ZoneId);
        }

        [Fact]
        public async Task RunAdaptiveAnalysis_ReturnsOkResult_WithMessage()
        {
            // Arrange
            _mockAdaptiveService.Setup(s => s.RunAdaptiveAnalysisAsync()).Returns(Task.CompletedTask);

            // Act
            var result = await _controller.RunAdaptiveAnalysis();

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var response = okResult.Value;
            Assert.NotNull(response);
            // The response is an anonymous object, so we can check the message property via dynamic or reflection
            var messageProperty = response.GetType().GetProperty("message");
            Assert.NotNull(messageProperty);
            Assert.Equal("Adaptive analysis completed", messageProperty.GetValue(response));
        }
    }
}