# Both stages pinned by digest (CKV_DOCKER_7), like the other services. They
# floated on 10.0 until 4.0.6, so the runtime was whatever the last build
# happened to pull: 4.0.5 shipped ASP.NET Core 10.0.12, with two critical CVEs
# (CVE-2026-88092, CVE-2026-70188; fixed in 10.0.13). Dependabot's docker
# ecosystem moves these forward from here, deliberately.
#
# 10.0.13 is under this estate's two-week rule (built 2026-10-21), taken
# anyway: the alternative is knowingly running two critical CVEs in a
# network-facing service. 10.0.14 (2026-10-26) was newer still; the older fix
# is the smaller bet.
#
# Build stage: SDK 10.0.402, the one that ships runtime 10.0.13.
FROM mcr.microsoft.com/dotnet/sdk:10.0.402@sha256:747cbade5bfd918a175ac32f8f1d931edec2c7d0d5397a3de72eaef1ac1f66d2 AS build
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
FROM mcr.microsoft.com/dotnet/aspnet:10.0.13@sha256:8af87eba5e7cdafb1514d7388be287365da97c132509e84dfe252ffd552398ca
WORKDIR /app
COPY --from=build /app/publish .

RUN chown -R app:app /app
USER app

EXPOSE 8080
ENTRYPOINT ["dotnet", "NineteenSeventyTwo.EightBitSaxLounge.Midi.MinimalApi.dll"]
