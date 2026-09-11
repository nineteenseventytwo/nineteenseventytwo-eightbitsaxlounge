# Changelog
## [4.0.5] - 2026-09-11

### Fixed
- `UploadEffects` blind-replaced each effect document with the config-file
  version (Name + Description only), destroying any `DeviceSettings` that
  `UploadDevice` had separately merged in. Re-running effects seeding after
  device seeding — which the migration's data-init playbooks now do
  routinely — silently wiped the per-device CC mappings for every engine
  effect and broke the chat bot's `!dial1`/`!dial2` commands with no error
  anywhere in the stack (midi returned 404 `Not Found` from `SetEffect`,
  logging `No device settings found for dependent effect '<X>' in device
  '<Y>'`). `!engine`/`Time`/`PreDelay` kept working because they don't read
  a dependent effect's `DeviceSettings`. Observed live in
  eightbitsaxlounge-dev on 2026-09-11 and fixed on the running pod by
  re-running `UploadDevice`; this release fixes the seeding endpoint itself
  so the fix doesn't need repeating. `UploadEffectsHandler` now updates the
  existing document in place (preserving `DeviceSettings`) instead of
  replacing it wholesale, matching the merge pattern `UploadDeviceHandler`
  already used.

## [4.0.4] - 2026-09-10

### Fixed
- The 4.0.3 push above never actually built or published anything: `midi-release.yaml` only triggers on changes to `midi/version.txt`, and the test-runner fix landed in a follow-up commit that didn't touch that file, so no new run fired. This bump exists purely to retrigger the release with the fix in place — no separate code change from 4.0.3.
- `dotnet test` was broken on .NET 10 SDK for this solution: xunit.v3 sets
  `IsTestingPlatformApplication=true`, which MSBuild's legacy `VSTest`
  target now hard-errors on ("Testing with VSTest target is no longer
  supported ... see https://aka.ms/dotnet-test-mtp-error"). This had never
  been exercised since the 4.0.2 .NET 10 upgrade because no release had
  run since. Fixed by declaring `"test": {"runner":
  "Microsoft.Testing.Platform"}` in midi/global.json (the directory the
  Makefile actually runs `dotnet` from) and switching the Makefile's test
  invocation to `dotnet test --solution` (the new CLI's required syntax
  for a solution path).

### Changed
- Version bump to publish the first container image under
  the `nineteenseventytwo` GHCR org (ADR-0012 platform migration); the repo
  transferred orgs after 4.0.2 was released.

## [4.0.2] - 2026-03-13

### Changed
- Upgraded target framework from `net8.0` to `net10.0` across all projects
- Docker build and runtime images updated to `dotnet/sdk:10.0` and `dotnet/aspnet:10.0`
- Runtime container switched from custom `midiuser` to built-in `app` user provided by the .NET 10 base image
- `global.json` SDK pin updated from `8.0.0` to `10.0.100` (rolls forward to latest 10.x minor)
- NuGet packages updated to .NET 10 releases: `CouchDB.NET` 4.0.0, `Dapper` 2.1.72, `JwtBearer` 10.0.4, `OpenApi` 10.0.4, `Swashbuckle.AspNetCore` 10.1.5, `Microsoft.Extensions.*` 10.0.4
- Makefile `test` and `lint` targets simplified: removed embedded `dotnet-install.sh` logic; `dotnet` resolved from PATH (set by CI before invoking make)
- GitHub Actions `test` and `build-pc` jobs install .NET to `$RUNNER_TOOL_CACHE/dotnet` (per-runner isolation) to prevent concurrent pipeline conflicts on shared self-hosted runners

### Fixed
- Resolved CS8603 null reference return warning in `EightBitSaxLoungeMidiDataService`

## [3.0.2] - 2026-03-10

### Added
- NATS publisher service (`INatsPublisher` interface + `NatsPublisher` implementation) for broadcasting overlay state from the MIDI layer
- `SetEffectHandler` now publishes overlay events after each successful effect change: `overlay.engine`, `overlay.predelay`, `overlay.time`, `overlay.control1`, `overlay.control2`
- Overlay events also published when resetting dependent effect settings
- `HandlerHelper` utility class for scaling MIDI control values (0–127) to display range (0–10) for overlay panels
- Unit tests for `HandlerHelper` value scaling (`HandlerHelperTests`)
- NATS configuration in `appsettings.json` and Kubernetes deployment (`NATS_URL`, `NATS_USER`, `NATS_PASS` env vars injected from `state-nats-creds` secret)

### Changed
- Major version bump (2.x → 3.x) reflects addition of outbound NATS overlay publishing
- `midi-api-deploy.yaml` updated to pass NATS secret reference

## [2.0.18] - 2026-02-12

### Changed
- Unified log format: `[timestamp] [Information] [midi] message correlationID=<id>`
- Correlation ID propagation from Chat to MIDI to Data
- Health check log exclusion for correlation ID tracking
- Grafana dashboard improvements for MIDI logs and health
- Version labels on pods for deployment tracking

## [2.0.13]

### Fixed
- resolve lint issues


## [2.0.12]

### Added
- ResetDevice endpoint (`POST /api/Midi/ResetDevice/{deviceName}`) to restore all device settings to defaults
- Resets feature: Effect settings can now specify dependent settings to reset when changed
- Unit tests for Resets feature (ResetsFeatureTests) covering automatic reset behavior
- Automatic pod restart in deployment workflow to pick up updated secrets
- Environment-specific deployment: All workflows now use explicit environment input (dev/prod) instead of branch-based detection

### Changed
- Data management workflows (data-init, data-upload, request-seteffect) now require environment selection
- Deployment workflow split environment variable assignment into conditional steps for proper secret interpolation
- ResetDevice optimization: Only sends MIDI control change messages for settings that differ from defaults
- Updated VentrisDualReverb configuration: ReverbEngineB Control1/Control2 default values changed from 0 to 10

### Fixed
- GitHub Actions secret interpolation in workflows by using YAML-level conditionals instead of bash if-statements
- Pod environment variables not updating when secrets change - added rollout restart to deployment

## [2.0.9]

### Added
- SetEffect endpoint (`POST /api/Midi/SetEffect`) for high-level device effect control
- SetEffectHandler to translate effect settings to MIDI control change messages
- SetEffectRequest record model with device, effect, and setting parameters
- Support for both value-based (int) and selection-based (string) effect settings
- GitHub Actions workflow for sending SetEffect requests (`midi-request-seteffect.yml`)
- Ansible playbook for SetEffect endpoint requests with authentication
- Make target `request-seteffect` for manual effect updates

## [2.0.8]

### Added
- Data initialization endpoint (`POST /api/Midi/InitializeDataModel`) to create CouchDB databases and views
- Data upload endpoints for effects (`POST /api/Midi/UploadEffects`) and devices (`POST /api/Midi/UploadDevice/{deviceName}`)
- Handler pattern architecture for endpoint logic separation
- Device configuration data model matching CouchDB schema (devices, effects, selectors)
- Configuration files: `appsettings.Effects.json` and `appsettings.Devices.VentrisDualReverb.json`
- GitHub Actions workflows for data initialization and upload
- Support for device-specific effect mappings with MIDI implementation details
- DeviceName field in effect device settings for multi-device support

### Changed
- Restructured data model to align with CouchDB document structure
- Updated effects configuration to include device-specific settings
- Refactored endpoint handlers into dedicated handler classes implementing `IEndpointHandler<TRequest, TResponse>`
- Enhanced data upload workflows with support for effects and devices upload types

## [1.0.7] - 2026-01-23

### Added
- Device proxy service for Kubernetes deployment
- HTTP-based MIDI device proxy with bypass key authentication
- Conditional service registration based on `MidiDeviceService:Url` configuration
- Support for both direct device access (Windows PC) and proxy mode (K8s container)
- Kubernetes deployment manifests (deployment, service, configmap, secret)
- Ansible-based deployment automation for K8s cluster
- Environment-specific device service URLs (dev: port 5000, prod: port 5001)
- GitHub Container Registry integration
- Consolidated CI/CD workflow for both PC and K8s deployments
- Bypass key authentication for internal K8s → Windows PC communication
- JWT token authentication requirement for external API requests

### Changed
- Split deployment architecture: K8s service for data endpoints + proxy to Windows PC for device control
- Updated endpoint handlers to support proxy pattern
- Enhanced health checks for containerized deployment
- Dockerfile updated to use .NET 8.0 (matching project target framework)
- Authentication middleware order to support bypass key only on Windows PC service
- K8s service requires JWT authentication, sends bypass key when proxying to Windows PC

### Fixed
- .NET runtime version mismatch in Docker container
- Missing token expiry configuration in K8s deployment
- Authentication flow for proxy requests
- Middleware execution order for bypass key validation

## [0.0.7] - 2026-01-18

### Added
- Initial MIDI service implementation
- Direct Windows MIDI device access via WinMM
- JWT authentication with client credentials
- Device control endpoints (SendControlChangeMessage, etc.)
- Data endpoints (placeholder for CouchDB integration)
- NSSM-based Windows service deployment
- Self-contained Windows x64 binary packaging
- GitHub Actions workflow for Windows PC deployment

[1.0.7]: https://github.com/mchellmer/nineteenseventytwo-eightbitsaxlounge/releases/tag/midi-api-v1.0.0
[0.0.7]: https://github.com/mchellmer/nineteenseventytwo-eightbitsaxlounge/releases/tag/midi-api-v0.0.7
