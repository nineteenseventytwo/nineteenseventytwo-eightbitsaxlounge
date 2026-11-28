# Both stages pinned by digest (CKV_DOCKER_7), like the other services. They
# floated on 10.0 until 4.0.6, so the runtime was whatever the last build
# happened to pull: 4.0.5 shipped ASP.NET Core 10.0.12, with two critical CVEs
# (CVE-2026-88092, CVE-2026-70188; fixed in 10.0.13). Dependabot's docker
# ecosystem moves these forward from here, deliberately.
#
# 10.0.14 (released 2026-10-25) fixes the Brotli decoder advisory
# GHSA-p3jp-9v83-76w9 in 10.0.13. 10.0.15 (2026-11-10) fixes three
# ASP.NET Core highs (CVE-2026-114146, -114147, -114149), so it is taken
# inside the two-week rule as a security fix.
#
# Build stage: SDK 10.0.404, the one that ships runtime 10.0.15.
FROM mcr.microsoft.com/dotnet/sdk:11.0.100@sha256:65f3c7c6a25a239ab3e6719f2896147e7b68242ff2d8818dd70498bde6ef8bea AS build
WORKDIR /src

# Copy solution and project files
COPY NineteenSeventyTwo.EightBitSaxLounge.Midi.sln ./
COPY NineteenSeventyTwo.EightBitSaxLounge.Midi/NineteenSeventyTwo.EightBitSaxLounge.Midi.MinimalApi/*.csproj ./NineteenSeventyTwo.EightBitSaxLounge.Midi/NineteenSeventyTwo.EightBitSaxLounge.Midi.MinimalApi/
COPY NineteenSeventyTwo.EightBitSaxLounge.Midi/NineteenSeventyTwo.EightBitSaxLounge.Midi.Library/*.csproj ./NineteenSeventyTwo.EightBitSaxLounge.Midi/NineteenSeventyTwo.EightBitSaxLounge.Midi.Library/
COPY NineteenSeventyTwo.EightBitSaxLounge.Midi/NineteenSeventyTwo.EightBitSaxLounge.Midi.UnitTests/*.csproj ./NineteenSeventyTwo.EightBitSaxLounge.Midi/NineteenSeventyTwo.EightBitSaxLounge.Midi.UnitTests/

# Restore dependencies
RUN dotnet restore

# Copy source code
COPY NineteenSeventyTwo.EightBitSaxLounge.Midi/NineteenSeventyTwo.EightBitSaxLounge.Midi.MinimalApi/ ./NineteenSeventyTwo.EightBitSaxLounge.Midi/NineteenSeventyTwo.EightBitSaxLounge.Midi.MinimalApi/
COPY NineteenSeventyTwo.EightBitSaxLounge.Midi/NineteenSeventyTwo.EightBitSaxLounge.Midi.Library/ ./NineteenSeventyTwo.EightBitSaxLounge.Midi/NineteenSeventyTwo.EightBitSaxLounge.Midi.Library/

# Build
WORKDIR /src/NineteenSeventyTwo.EightBitSaxLounge.Midi/NineteenSeventyTwo.EightBitSaxLounge.Midi.MinimalApi
RUN dotnet publish -c Release -o /app/publish --no-restore

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:11.0.0@sha256:bc0d6653979efba491ab3d6f589d691f96c309c3c3ef81325efa6978e7b7f242

# apt-get upgrade: the Ubuntu base ships apt without noble-security's fix
# (CVE-2026-109518). Release builds have no layer cache, so every release
# picks up what Ubuntu has fixed since the base was built.
RUN apt-get update \
    && apt-get upgrade -y \
    && rm -rf /var/lib/apt/lists/*
WORKDIR /app
COPY --from=build /app/publish .

RUN chown -R app:app /app
USER app

EXPOSE 8080
ENTRYPOINT ["dotnet", "NineteenSeventyTwo.EightBitSaxLounge.Midi.MinimalApi.dll"]
