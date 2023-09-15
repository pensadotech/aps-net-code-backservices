FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build-env
WORKDIR /app

COPY . .

RUN dotnet restore ./src/TennisBookings.ScoreProcessor/TennisBookings.ScoreProcessor.csproj
RUN dotnet restore ./src/TennisBookings.ResultsProcessing/TennisBookings.ResultsProcessing.csproj

# Build and publish a release

RUN dotnet publish ./src/TennisBookings.ScoreProcessor/TennisBookings.ScoreProcessor.csproj -c Release -o out

# Build runtime image
FROM mcr.microsoft.com/dotnet/runtime:6.0
WORKDIR /app
COPY --from=build-env /app/out .
ENTRYPOINT [ "dotnet", "TennisBookings.ScoreProcessor.dll" ]