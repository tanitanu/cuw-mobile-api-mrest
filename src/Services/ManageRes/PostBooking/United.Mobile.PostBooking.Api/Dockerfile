#See https://aka.ms/containerfastmode to understand how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM artifactorycloud.ual.com/l-docker-releases/datadog/dotnet/aspnet:6.0.5-2.10.0 AS base

WORKDIR /app
EXPOSE 80

FROM artifactorycloud.ual.com/l-docker/ms/dotnet/core/sdk:6.0 AS build
WORKDIR /

COPY ["src/Services/ManageRes/PostBooking/United.Mobile.PostBooking.Api/United.Mobile.PostBooking.Api.csproj", "src/Services/ManageRes/PostBooking/United.Mobile.PostBooking.Api/"]
COPY ["src/Core/United.Utility/United.Utility.csproj", "src/Core/United.Utility/"]
COPY ["src/Core/United.Ebs.Logging/United.Ebs.Logging.csproj", "src/Core/United.Ebs.Logging/"]
COPY ["src/Model/United.Mobile.Model.Common/United.Mobile.Model.Common.csproj", "src/Model/United.Mobile.Model.Common/"]
COPY ["src/Model/SeatEngine/United.Mobile.Model.SeatEngine/United.Mobile.Model.SeatEngine.csproj", "src/Model/SeatEngine/United.Mobile.Model.SeatEngine/"]
COPY ["src/Model.Internal/United.Mobile.Model.Internal.Exception/United.Mobile.Model.Internal.Exception.csproj", "src/Model.Internal/United.Mobile.Model.Internal.Exception/"]
COPY ["src/Core/United.Utility.Middleware/United.Utility.Middleware.csproj", "src/Core/United.Utility.Middleware/"]
COPY ["src/Services/ManageRes/PostBooking/United.Mobile.PostBooking.Domain/United.Mobile.PostBooking.Domain.csproj", "src/Services/ManageRes/PostBooking/United.Mobile.PostBooking.Domain/"]
COPY ["src/DataAccess/United.Mobile.DataAccess.Common/United.Mobile.DataAccess.Common.csproj", "src/DataAccess/United.Mobile.DataAccess.Common/"]
COPY ["src/Model.Internal/United.Definition/United.Mobile.Model.Internal.Common.csproj", "src/Model.Internal/United.Definition/"]
COPY ["src/Model.Internal/United.Mobile.Model.Internal/United.Mobile.Model.Internal.csproj", "src/Model.Internal/United.Mobile.Model.Internal/"]
COPY ["src/Model/eRes/United.Mobile.Model.HomePageContent/United.Mobile.Model.HomePageContent.csproj", "src/Model/eRes/United.Mobile.Model.HomePageContent/"]
COPY ["src/Model/ManageRes/United.Mobile.Model.PostBooking/United.Mobile.Model.PostBooking.csproj", "src/Model/ManageRes/United.Mobile.Model.PostBooking/"]
COPY ["src/Model/Shopping/United.Mobile.Model.Shopping/United.Mobile.Model.Shopping.csproj", "src/Model/Shopping/United.Mobile.Model.Shopping/"]
COPY ["src/Model/Shopping/United.Mobile.Model.ShopAward/United.Mobile.Model.ShopAward.csproj", "src/Model/Shopping/United.Mobile.Model.ShopAward/"]
COPY ["src/Model/Shopping/United.Mobile.Model.Common/United.Mobile.Model.Shopping.Common.csproj", "src/Model/Shopping/United.Mobile.Model.Common/"]
COPY ["src/Model/Ancillary/United.Mobile.Model.Catalog/United.Mobile.Model.Catalog.csproj", "src/Model/Ancillary/United.Mobile.Model.Catalog/"]
COPY ["src/Model/PNRManagement/United.Mobile.Model.PNRManagement/United.Mobile.Model.PNRManagement.csproj", "src/Model/PNRManagement/United.Mobile.Model.PNRManagement/"]
COPY ["src/Model/FlightStatus/United.Mobile.Model.FlightStatus/United.Mobile.Model.FlightStatus.csproj", "src/Model/FlightStatus/United.Mobile.Model.FlightStatus/"]
COPY ["src/Model/AccountManagement/United.Mobile.Model.MPRewards/United.Mobile.Model.MPRewards.csproj", "src/Model/AccountManagement/United.Mobile.Model.MPRewards/"]
COPY ["src/Model/Ancillary/United.Mobile.Model.Fitbit/United.Mobile.Model.Fitbit.csproj", "src/Model/Ancillary/United.Mobile.Model.Fitbit/"]
COPY ["src/Model/Ancillary/United.Mobile.Model.FeedBack/United.Mobile.Model.FeedBack.csproj", "src/Model/Ancillary/United.Mobile.Model.FeedBack/"]
COPY ["src/Model/AccountManagement/United.Mobile.Model.AccountManagement/United.Mobile.Model.MPSignIn.csproj", "src/Model/AccountManagement/United.Mobile.Model.AccountManagement/"]
COPY ["src/Model/Shopping/United.Mobile.Model.Booking/United.Mobile.Model.Booking.csproj", "src/Model/Shopping/United.Mobile.Model.Booking/"]
COPY ["src/Model/AccountManagement/United.Mobile.Model.UpdateMemberProfile/United.Mobile.Model.UpdateMemberProfile.csproj", "src/Model/AccountManagement/United.Mobile.Model.UpdateMemberProfile/"]
COPY ["src/Model/AccountManagement/United.Mobile.Model.MemberProfile/United.Mobile.Model.MemberProfile.csproj", "src/Model/AccountManagement/United.Mobile.Model.MemberProfile/"]
COPY ["src/Model/Shopping/United.Mobile.Model.EmpShop/United.Mobile.Model.EmpShop.csproj", "src/Model/Shopping/United.Mobile.Model.EmpShop/"]

ARG NUGET_PASSWORD
ARG NUGET_USERNAME


RUN dotnet nuget add source https://artifactorycloud.ual.com/artifactory/api/nuget/v3/v-nuget \
            -n artifactory \
            -u ${NUGET_USERNAME}  \
            -p ${NUGET_PASSWORD} \
            --store-password-in-clear-text


COPY . .
WORKDIR "/src/Services/ManageRes/PostBooking/United.Mobile.PostBooking.Api"

FROM build AS publish
RUN dotnet publish "United.Mobile.PostBooking.Api.csproj" -c Release --self-contained -r linux-x64 -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "United.Mobile.PostBooking.Api.dll"]