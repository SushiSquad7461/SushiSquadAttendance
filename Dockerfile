FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build-env
WORKDIR SushiSquadAttendance

# Copy everything else and build
COPY . .
RUN dotnet restore
RUN dotnet publish -c Release -o out

# Build runtime image
FROM mcr.microsoft.com/dotnet/runtime:6.0
WORKDIR SushiSquadAttendance
COPY --from=build-env /SushiSquadAttendance/token .
COPY --from=build-env /SushiSquadAttendance/out .

# Run the app on container startup
ENTRYPOINT [ "dotnet", "SushiSquadAttendance.dll" ]