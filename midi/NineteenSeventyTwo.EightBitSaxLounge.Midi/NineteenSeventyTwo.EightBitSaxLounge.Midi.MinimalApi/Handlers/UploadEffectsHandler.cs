using NineteenSeventyTwo.EightBitSaxLounge.Midi.Library.DataAccess;
using NineteenSeventyTwo.EightBitSaxLounge.Midi.Library.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NineteenSeventyTwo.EightBitSaxLounge.Midi.MinimalApi.Models;

namespace NineteenSeventyTwo.EightBitSaxLounge.Midi.MinimalApi.Handlers;

public class UploadEffectsHandler : IMidiEndpointHandler<IResult>
{
    private readonly ILogger<UploadEffectsHandler> _logger;
    private readonly IMidiDataService _midiDataService;
    private readonly IOptions<EffectsOptions> _effectsOptions;

    public UploadEffectsHandler(
        ILogger<UploadEffectsHandler> logger,
        IMidiDataService midiDataService,
        IOptions<EffectsOptions> effectsOptions)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _midiDataService = midiDataService ?? throw new ArgumentNullException(nameof(midiDataService));
        _effectsOptions = effectsOptions ?? throw new ArgumentNullException(nameof(effectsOptions));
    }

    public async Task<IResult> HandleAsync()
    {
        _logger.LogInformation("Starting effects upload");

        var effectsFromConfig = _effectsOptions.Value.Effects;
        if (effectsFromConfig.Count == 0)
        {
            _logger.LogWarning("No effects found in configuration");
            return Results.Ok(new { Message = "No effects found in configuration to upload" });
        }

        _logger.LogInformation($"Found {effectsFromConfig.Count} effects in configuration");

        try
        {
            // Get all existing effects from database
            List<Effect> existingEffects;
            try
            {
                existingEffects = await _midiDataService.GetAllEffectsAsync();
                _logger.LogInformation($"Retrieved {existingEffects.Count} existing effects from database");
            }
            catch (Exception e)
            {
                _logger.LogWarning($"Could not retrieve existing effects (database may be empty): {e.Message}");
                existingEffects = new List<Effect>();
            }

            int created = 0;
            int updated = 0;
            int failed = 0;
            var errors = new List<string>();

            foreach (var effect in effectsFromConfig)
            {
                try
                {
                    var existingEffect = existingEffects.FirstOrDefault(e =>
                        e.Name.Equals(effect.Name, StringComparison.OrdinalIgnoreCase));

                    if (existingEffect != null)
                    {
                        _logger.LogInformation($"Updating effect: {effect.Name}");
                        // Do not persist `effect` as-is: it comes from
                        // appsettings.Effects.json, which only ever carries
                        // Name/Description. This config has no idea about
                        // DeviceSettings (the per-device CC mappings that
                        // UploadDevice separately merges in), so a blind
                        // replace here wipes them out from under it. Update
                        // the existing document in place instead, the same
                        // way UploadDeviceHandler merges rather than
                        // replaces. Re-running this endpoint after
                        // UploadDevice previously did exactly that: it
                        // silently deleted every DeviceSettings entry and
                        // broke every effect that depends on one (e.g. the
                        // dial1/dial2 chat commands, which read Control1/
                        // Control2 off the current engine's DeviceSettings).
                        existingEffect.Description = effect.Description;
                        await _midiDataService.UpdateEffectByNameAsync(effect.Name, existingEffect);
                        updated++;
                        _logger.LogInformation($"Effect updated successfully: {effect.Name}");
                    }
                    else
                    {
                        _logger.LogInformation($"Creating effect: {effect.Name}");
                        await _midiDataService.CreateEffectAsync(effect.Name, effect);
                        created++;
                        _logger.LogInformation($"Effect created successfully: {effect.Name}");
                    }
                }
                catch (Exception e)
                {
                    failed++;
                    var errorMsg = $"Failed to process effect '{effect.Name}': {e.Message}";
                    _logger.LogError(e, errorMsg);
                    errors.Add(errorMsg);
                }
            }

            var summary = $"Effects upload completed. Created: {created}, Updated: {updated}, Failed: {failed}";
            _logger.LogInformation(summary);

            if (failed > 0)
            {
                return Results.Problem(
                    detail: $"{summary}. Errors: {string.Join("; ", errors)}",
                    title: "Effects upload completed with errors",
                    statusCode: 500);
            }

            return Results.Ok(new { Message = summary, Created = created, Updated = updated });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during effects upload");
            return Results.Problem(
                detail: $"Effects upload failed: {ex.Message}",
                title: "Effects upload failed",
                statusCode: 500);
        }
    }
}
