#See https://aka.ms/containerfastmode to understand how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM artifactorycloud.ual.com/l-docker-releases/datadog/dotnet/aspnet:6.0.5-2.10.0 AS base

WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM artifactorycloud.ual.com/l-docker/ms/dotnet/core/sdk:6.0 AS build
WORKDIR /

COPY ["src/Services/Shopping/TripPlannerService/United.Mobile.Services.TripPlannerService.Api/United.Mobile.Services.TripPlannerService.Api.csproj", "src/Services/Shopping/TripPlannerService/United.Mobile.Services.TripPlannerService.Api/"]

ARG NUGET_PASSWORD
ARG NUGET_USERNAME


RUN dotnet nuget add source https://artifactorycloud.ual.com/artifactory/api/nuget/v3/v-nuget \
            -n artifactory \
            -u ${NUGET_USERNAME}  \
            -p ${NUGET_PASSWORD} \
            --store-password-in-clear-text


COPY . .
WORKDIR "/src/Services/Shopping/TripPlannerService/United.Mobile.Services.TripPlannerService.Api"

FROM build AS publish
RUN dotnet publish "United.Mobile.Services.TripPlannerService.Api.csproj" -c Release --self-contained -r linux-x64 -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "United.Mobile.Services.TripPlannerService.Api.dll"]