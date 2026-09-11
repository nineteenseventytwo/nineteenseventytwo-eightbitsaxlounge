using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using NineteenSeventyTwo.EightBitSaxLounge.Midi.Library.DataAccess;
using NineteenSeventyTwo.EightBitSaxLounge.Midi.Library.Models;
using NineteenSeventyTwo.EightBitSaxLounge.Midi.MinimalApi.Handlers;
using NineteenSeventyTwo.EightBitSaxLounge.Midi.MinimalApi.Models;

namespace NineteenSeventyTwo.EightBitSaxLounge.Midi.UnitTests.EndpointTests;

public class UploadEffectsHandlerTests : TestBase
{
    [Fact]
    public async Task UploadEffects_NoEffectsInConfig_Returns200()
    {
        // Arrange
        var loggerMock = new Mock<ILogger<UploadEffectsHandler>>();
        var dataServiceMock = new Mock<IMidiDataService>();

        var effectsOptions = Options.Create(new EffectsOptions { Effects = new List<Effect>() });
        var handler = new UploadEffectsHandler(loggerMock.Object, dataServiceMock.Object, effectsOptions);

        // Act
        var result = await handler.HandleAsync();
        var (status, body) = await ExecuteResultAsync(result);

        // Assert
        Assert.Equal(200, status);
        Assert.Contains("No effects found in configuration", body);

        dataServiceMock.Verify(m => m.GetAllEffectsAsync(), Times.Never);
        dataServiceMock.Verify(m => m.CreateEffectAsync(It.IsAny<string>(), It.IsAny<Effect>()), Times.Never);
        dataServiceMock.Verify(m => m.UpdateEffectByNameAsync(It.IsAny<string>(), It.IsAny<Effect>()), Times.Never);
    }

    [Fact]
    public async Task UploadEffects_AllNewEffects_CreatesAll()
    {
        // Arrange
        var loggerMock = new Mock<ILogger<UploadEffectsHandler>>();
        var dataServiceMock = new Mock<IMidiDataService>();

        var effects = new List<Effect>
        {
            new() { Name = "Reverb", Description = "Reverb effect" },
            new() { Name = "Delay", Description = "Delay effect" },
            new() { Name = "Chorus", Description = "Chorus effect" }
        };

        var effectsOptions = Options.Create(new EffectsOptions { Effects = effects });

        // Empty database - no existing effects
        dataServiceMock.Setup(m => m.GetAllEffectsAsync()).ReturnsAsync(new List<Effect>());

        dataServiceMock.Setup(m => m.CreateEffectAsync("Reverb", It.IsAny<Effect>())).Returns(Task.CompletedTask);
        dataServiceMock.Setup(m => m.CreateEffectAsync("Delay", It.IsAny<Effect>())).Returns(Task.CompletedTask);
        dataServiceMock.Setup(m => m.CreateEffectAsync("Chorus", It.IsAny<Effect>())).Returns(Task.CompletedTask);

        var handler = new UploadEffectsHandler(loggerMock.Object, dataServiceMock.Object, effectsOptions);

        // Act
        var result = await handler.HandleAsync();
        var (status, body) = await ExecuteResultAsync(result);

        // Assert
        Assert.Equal(200, status);
        Assert.Contains("Created: 3", body);
        Assert.Contains("Updated: 0", body);

        dataServiceMock.Verify(m => m.GetAllEffectsAsync(), Times.Once);
        dataServiceMock.Verify(m => m.CreateEffectAsync("Reverb", It.Is<Effect>(e => e.Name == "Reverb")), Times.Once);
        dataServiceMock.Verify(m => m.CreateEffectAsync("Delay", It.Is<Effect>(e => e.Name == "Delay")), Times.Once);
        dataServiceMock.Verify(m => m.CreateEffectAsync("Chorus", It.Is<Effect>(e => e.Name == "Chorus")), Times.Once);
        dataServiceMock.Verify(m => m.UpdateEffectByNameAsync(It.IsAny<string>(), It.IsAny<Effect>()), Times.Never);
    }

    [Fact]
    public async Task UploadEffects_AllExistingEffects_UpdatesAll()
    {
        // Arrange
        var loggerMock = new Mock<ILogger<UploadEffectsHandler>>();
        var dataServiceMock = new Mock<IMidiDataService>();

        var effects = new List<Effect>
        {
            new() { Name = "Reverb", Description = "Updated reverb effect" },
            new() { Name = "Delay", Description = "Updated delay effect" }
        };

        var effectsOptions = Options.Create(new EffectsOptions { Effects = effects });

        // Existing effects in database
        var existingEffects = new List<Effect>
        {
            new() { Name = "Reverb", Description = "Old reverb effect" },
            new() { Name = "Delay", Description = "Old delay effect" }
        };

        dataServiceMock.Setup(m => m.GetAllEffectsAsync()).ReturnsAsync(existingEffects);

        dataServiceMock.Setup(m => m.UpdateEffectByNameAsync("Reverb", It.IsAny<Effect>())).Returns(Task.CompletedTask);
        dataServiceMock.Setup(m => m.UpdateEffectByNameAsync("Delay", It.IsAny<Effect>())).Returns(Task.CompletedTask);

        var handler = new UploadEffectsHandler(loggerMock.Object, dataServiceMock.Object, effectsOptions);

        // Act
        var result = await handler.HandleAsync();
        var (status, body) = await ExecuteResultAsync(result);

        // Assert
        Assert.Equal(200, status);
        Assert.Contains("Created: 0", body);
        Assert.Contains("Updated: 2", body);

        dataServiceMock.Verify(m => m.GetAllEffectsAsync(), Times.Once);
        dataServiceMock.Verify(m => m.UpdateEffectByNameAsync("Reverb", It.Is<Effect>(e => e.Description == "Updated reverb effect")), Times.Once);
        dataServiceMock.Verify(m => m.UpdateEffectByNameAsync("Delay", It.Is<Effect>(e => e.Description == "Updated delay effect")), Times.Once);
        dataServiceMock.Verify(m => m.CreateEffectAsync(It.IsAny<string>(), It.IsAny<Effect>()), Times.Never);
    }

    [Fact]
    public async Task UploadEffects_ExistingEffectHasDeviceSettings_PreservesThemOnUpdate()
    {
        // Regression test: UploadDevice merges per-device CC mappings into
        // DeviceSettings, but appsettings.Effects.json only ever carries
        // Name/Description. Re-running UploadEffects after UploadDevice
        // used to blind-replace the whole document and silently delete
        // DeviceSettings, breaking every effect that depends on one (e.g.
        // dial1/dial2, which read Control1/Control2 off the current
        // engine's DeviceSettings). Observed live in eightbitsaxlounge-dev
        // on 2026-09-11.
        // Arrange
        var loggerMock = new Mock<ILogger<UploadEffectsHandler>>();
        var dataServiceMock = new Mock<IMidiDataService>();

        var effects = new List<Effect>
        {
            new() { Name = "Hall", Description = "Updated hall description" }
        };

        var effectsOptions = Options.Create(new EffectsOptions { Effects = effects });

        var deviceSettings = new List<DeviceSetting>
        {
            new() { Name = "Control1", DeviceName = "VentrisDualReverb", EffectName = "Bass" },
            new() { Name = "Control2", DeviceName = "VentrisDualReverb", EffectName = "Size" }
        };
        var existingEffects = new List<Effect>
        {
            new() { Name = "Hall", Description = "Old hall description", DeviceSettings = deviceSettings }
        };

        dataServiceMock.Setup(m => m.GetAllEffectsAsync()).ReturnsAsync(existingEffects);
        dataServiceMock.Setup(m => m.UpdateEffectByNameAsync("Hall", It.IsAny<Effect>())).Returns(Task.CompletedTask);

        var handler = new UploadEffectsHandler(loggerMock.Object, dataServiceMock.Object, effectsOptions);

        // Act
        var result = await handler.HandleAsync();
        var (status, _) = await ExecuteResultAsync(result);

        // Assert
        Assert.Equal(200, status);
        dataServiceMock.Verify(m => m.UpdateEffectByNameAsync("Hall", It.Is<Effect>(e =>
            e.Description == "Updated hall description" &&
            e.DeviceSettings != null &&
            e.DeviceSettings.Count == 2 &&
            e.DeviceSettings.Any(ds => ds.Name == "Control1" && ds.EffectName == "Bass"))), Times.Once);
    }

    [Fact]
    public async Task UploadEffects_MixedEffects_CreatesAndUpdates()
    {
        // Arrange
        var loggerMock = new Mock<ILogger<UploadEffectsHandler>>();
        var dataServiceMock = new Mock<IMidiDataService>();

        var effects = new List<Effect>
        {
            new() { Name = "Reverb", Description = "Updated reverb effect" },
            new() { Name = "Delay", Description = "New delay effect" },
            new() { Name = "Chorus", Description = "New chorus effect" }
        };

        var effectsOptions = Options.Create(new EffectsOptions { Effects = effects });

        // Only Reverb exists in database
        var existingEffects = new List<Effect>
        {
            new() { Name = "Reverb", Description = "Old reverb effect" }
        };

        dataServiceMock.Setup(m => m.GetAllEffectsAsync()).ReturnsAsync(existingEffects);

        dataServiceMock.Setup(m => m.UpdateEffectByNameAsync("Reverb", It.IsAny<Effect>())).Returns(Task.CompletedTask);
        dataServiceMock.Setup(m => m.CreateEffectAsync("Delay", It.IsAny<Effect>())).Returns(Task.CompletedTask);
        dataServiceMock.Setup(m => m.CreateEffectAsync("Chorus", It.IsAny<Effect>())).Returns(Task.CompletedTask);

        var handler = new UploadEffectsHandler(loggerMock.Object, dataServiceMock.Object, effectsOptions);

        // Act
        var result = await handler.HandleAsync();
        var (status, body) = await ExecuteResultAsync(result);

        // Assert
        Assert.Equal(200, status);
        Assert.Contains("Created: 2", body);
        Assert.Contains("Updated: 1", body);

        dataServiceMock.Verify(m => m.GetAllEffectsAsync(), Times.Once);
        dataServiceMock.Verify(m => m.UpdateEffectByNameAsync("Reverb", It.IsAny<Effect>()), Times.Once);
        dataServiceMock.Verify(m => m.CreateEffectAsync("Delay", It.IsAny<Effect>()), Times.Once);
        dataServiceMock.Verify(m => m.CreateEffectAsync("Chorus", It.IsAny<Effect>()), Times.Once);
    }

    [Fact]
    public async Task UploadEffects_DatabaseEmpty_CreatesAll()
    {
        // Arrange
        var loggerMock = new Mock<ILogger<UploadEffectsHandler>>();
        var dataServiceMock = new Mock<IMidiDataService>();

        var effects = new List<Effect>
        {
            new() { Name = "Reverb", Description = "Reverb effect" }
        };

        var effectsOptions = Options.Create(new EffectsOptions { Effects = effects });

        // GetAllEffectsAsync throws (database doesn't exist or is empty)
        dataServiceMock.Setup(m => m.GetAllEffectsAsync()).ThrowsAsync(new Exception("Database empty"));

        dataServiceMock.Setup(m => m.CreateEffectAsync("Reverb", It.IsAny<Effect>())).Returns(Task.CompletedTask);

        var handler = new UploadEffectsHandler(loggerMock.Object, dataServiceMock.Object, effectsOptions);

        // Act
        var result = await handler.HandleAsync();
        var (status, body) = await ExecuteResultAsync(result);

        // Assert
        Assert.Equal(200, status);
        Assert.Contains("Created: 1", body);

        dataServiceMock.Verify(m => m.GetAllEffectsAsync(), Times.Once);
        dataServiceMock.Verify(m => m.CreateEffectAsync("Reverb", It.IsAny<Effect>()), Times.Once);
    }

    [Fact]
    public async Task UploadEffects_CreateFails_Returns500WithErrors()
    {
        // Arrange
        var loggerMock = new Mock<ILogger<UploadEffectsHandler>>();
        var dataServiceMock = new Mock<IMidiDataService>();

        var effects = new List<Effect>
        {
            new() { Name = "Reverb", Description = "Reverb effect" },
            new() { Name = "Delay", Description = "Delay effect" }
        };

        var effectsOptions = Options.Create(new EffectsOptions { Effects = effects });

        dataServiceMock.Setup(m => m.GetAllEffectsAsync()).ReturnsAsync(new List<Effect>());

        dataServiceMock.Setup(m => m.CreateEffectAsync("Reverb", It.IsAny<Effect>()))
            .ThrowsAsync(new Exception("Create failed"));
        dataServiceMock.Setup(m => m.CreateEffectAsync("Delay", It.IsAny<Effect>()))
            .Returns(Task.CompletedTask);

        var handler = new UploadEffectsHandler(loggerMock.Object, dataServiceMock.Object, effectsOptions);

        // Act
        var result = await handler.HandleAsync();
        var (status, body) = await ExecuteResultAsync(result);

        // Assert
        Assert.Equal(500, status);
        Assert.Contains("Effects upload completed with errors", body);
        Assert.Contains("Created: 1", body);
        Assert.Contains("Failed: 1", body);
        Assert.Contains("Reverb", body);

        dataServiceMock.Verify(m => m.CreateEffectAsync("Reverb", It.IsAny<Effect>()), Times.Once);
        dataServiceMock.Verify(m => m.CreateEffectAsync("Delay", It.IsAny<Effect>()), Times.Once);
    }

    [Fact]
    public async Task UploadEffects_UpdateFails_Returns500WithErrors()
    {
        // Arrange
        var loggerMock = new Mock<ILogger<UploadEffectsHandler>>();
        var dataServiceMock = new Mock<IMidiDataService>();

        var effects = new List<Effect>
        {
            new() { Name = "Reverb", Description = "Updated reverb effect" }
        };

        var effectsOptions = Options.Create(new EffectsOptions { Effects = effects });

        var existingEffects = new List<Effect>
        {
            new() { Name = "Reverb", Description = "Old reverb effect" }
        };

        dataServiceMock.Setup(m => m.GetAllEffectsAsync()).ReturnsAsync(existingEffects);

        dataServiceMock.Setup(m => m.UpdateEffectByNameAsync("Reverb", It.IsAny<Effect>()))
            .ThrowsAsync(new Exception("Update failed"));

        var handler = new UploadEffectsHandler(loggerMock.Object, dataServiceMock.Object, effectsOptions);

        // Act
        var result = await handler.HandleAsync();
        var (status, body) = await ExecuteResultAsync(result);

        // Assert
        Assert.Equal(500, status);
        Assert.Contains("Effects upload completed with errors", body);
        Assert.Contains("Failed: 1", body);
        Assert.Contains("Reverb", body);

        dataServiceMock.Verify(m => m.UpdateEffectByNameAsync("Reverb", It.IsAny<Effect>()), Times.Once);
    }

    [Fact]
    public async Task UploadEffects_CaseInsensitiveMatch_Updates()
    {
        // Arrange
        var loggerMock = new Mock<ILogger<UploadEffectsHandler>>();
        var dataServiceMock = new Mock<IMidiDataService>();

        var effects = new List<Effect>
        {
            new() { Name = "REVERB", Description = "Updated reverb effect" }
        };

        var effectsOptions = Options.Create(new EffectsOptions { Effects = effects });

        // Existing effect with different case
        var existingEffects = new List<Effect>
        {
            new() { Name = "reverb", Description = "Old reverb effect" }
        };

        dataServiceMock.Setup(m => m.GetAllEffectsAsync()).ReturnsAsync(existingEffects);

        dataServiceMock.Setup(m => m.UpdateEffectByNameAsync("REVERB", It.IsAny<Effect>())).Returns(Task.CompletedTask);

        var handler = new UploadEffectsHandler(loggerMock.Object, dataServiceMock.Object, effectsOptions);

        // Act
        var result = await handler.HandleAsync();
        var (status, body) = await ExecuteResultAsync(result);

        // Assert
        Assert.Equal(200, status);
        Assert.Contains("Updated: 1", body);
        Assert.Contains("Created: 0", body);

        dataServiceMock.Verify(m => m.UpdateEffectByNameAsync("REVERB", It.IsAny<Effect>()), Times.Once);
        dataServiceMock.Verify(m => m.CreateEffectAsync(It.IsAny<string>(), It.IsAny<Effect>()), Times.Never);
    }
}
