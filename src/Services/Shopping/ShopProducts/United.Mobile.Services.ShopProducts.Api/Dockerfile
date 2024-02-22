#See https://aka.ms/containerfastmode to understand how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM artifactorycloud.ual.com/l-docker/datadog/dotnet/runtime:3.1-1.28.7 AS base

WORKDIR /app
EXPOSE 80

FROM artifactorycloud.ual.com/l-docker/ms/dotnet/core/sdk:3.1 AS build
WORKDIR /

COPY ["src/Services/Shopping/ShopProducts/United.Mobile.Services.ShopProducts.Api/United.Mobile.Services.ShopProducts.Api.csproj", "src/Services/Shopping/ShopProducts/United.Mobile.Services.ShopProducts.Api/"]
COPY ["src/Services/Shopping/ShopProducts/United.Mobile.Services.ShopProducts.Domain/United.Mobile.Services.ShopProducts.Domain.csproj", "src/Services/Shopping/ShopProducts/United.Mobile.Services.ShopProducts.Domain/"]
COPY ["src/DataAccess/Shopping/United.Mobile.DataAccess.ShopProducts/United.Mobile.DataAccess.ShopProducts.csproj", "src/DataAccess/Shopping/United.Mobile.DataAccess.ShopProducts/"]
COPY ["src/Core/United.Utility/United.Utility.csproj", "src/Core/United.Utility/"]
COPY ["src/Core/United.Ebs.Logging/United.Ebs.Logging.csproj", "src/Core/United.Ebs.Logging/"]
COPY ["src/Model/United.Mobile.Model.Common/United.Mobile.Model.Common.csproj", "src/Model/United.Mobile.Model.Common/"]
COPY ["src/Model/SeatEngine/United.Mobile.Model.SeatEngine/United.Mobile.Model.SeatEngine.csproj", "src/Model/SeatEngine/United.Mobile.Model.SeatEngine/"]
COPY ["src/Model.Internal/United.Mobile.Model.Internal.Exception/United.Mobile.Model.Internal.Exception.csproj", "src/Model.Internal/United.Mobile.Model.Internal.Exception/"]
COPY ["src/DataAccess/United.Mobile.DataAccess.FlightShopping/United.Mobile.DataAccess.FlightShopping.csproj", "src/DataAccess/United.Mobile.DataAccess.FlightShopping/"]
COPY ["src/Core/Common/United.Common.Helper.Shopping/United.Common.Helper.Shopping.csproj", "src/Core/Common/United.Common.Helper.Shopping/"]
COPY ["src/DataAccess/United.Mobile.DataAccess.Common/United.Mobile.DataAccess.Common.csproj", "src/DataAccess/United.Mobile.DataAccess.Common/"]
COPY ["src/Model.Internal/United.Definition/United.Mobile.Model.Internal.Common.csproj", "src/Model.Internal/United.Definition/"]
COPY ["src/Model.Internal/United.Mobile.Model.Internal/United.Mobile.Model.Internal.csproj", "src/Model.Internal/United.Mobile.Model.Internal/"]
COPY ["src/Model/eRes/United.Mobile.Model.HomePageContent/United.Mobile.Model.HomePageContent.csproj", "src/Model/eRes/United.Mobile.Model.HomePageContent/"]
COPY ["src/Model/Shopping/United.Mobile.Model.Shopping/United.Mobile.Model.Shopping.csproj", "src/Model/Shopping/United.Mobile.Model.Shopping/"]
COPY ["src/Model/Shopping/United.Mobile.Model.ShopAward/United.Mobile.Model.ShopAward.csproj", "src/Model/Shopping/United.Mobile.Model.ShopAward/"]
COPY ["src/Model/Shopping/United.Mobile.Model.Common/United.Mobile.Model.Shopping.Common.csproj", "src/Model/Shopping/United.Mobile.Model.Common/"]
COPY ["src/Model/PNRManagement/United.Mobile.Model.PNRManagement/United.Mobile.Model.PNRManagement.csproj", "src/Model/PNRManagement/United.Mobile.Model.PNRManagement/"]
COPY ["src/Model/FlightStatus/United.Mobile.Model.FlightStatus/United.Mobile.Model.FlightStatus.csproj", "src/Model/FlightStatus/United.Mobile.Model.FlightStatus/"]
COPY ["src/Model/Payment/United.Mobile.Model.Payment/United.Mobile.Model.Payment.csproj", "src/Model/Payment/United.Mobile.Model.Payment/"]
COPY ["src/DataAccess/Shopping/United.Mobile.DataAccess.ShopTrips/United.Mobile.DataAccess.ShopTrips.csproj", "src/DataAccess/Shopping/United.Mobile.DataAccess.ShopTrips/"]
COPY ["src/Core/United.Common.Helper/United.Common.Helper.csproj", "src/Core/United.Common.Helper/"]
COPY ["src/DataAccess/United.Mobile.DataAccess.DynamoDB/United.Mobile.DataAccess.DynamoDB.csproj", "src/DataAccess/United.Mobile.DataAccess.DynamoDB/"]
COPY ["src/DataAccess/BagCalculator/United.Mobile.DataAccess.CMSContent/United.Mobile.DataAccess.CMSContent.csproj", "src/DataAccess/BagCalculator/United.Mobile.DataAccess.CMSContent/"]
COPY ["src/Model.Internal/AccountManagement/United.Mobile.Model.Internal.AccountManagement/United.Mobile.Model.Internal.AccountManagement.csproj", "src/Model.Internal/AccountManagement/United.Mobile.Model.Internal.AccountManagement/"]
COPY ["src/Model/Shopping/United.Mobile.Model.EmpShop/United.Mobile.Model.EmpShop.csproj", "src/Model/Shopping/United.Mobile.Model.EmpShop/"]
COPY ["src/DataAccess/AccountManagement/United.Mobile.DataAccess.Customer/United.Mobile.DataAccess.Customer.csproj", "src/DataAccess/AccountManagement/United.Mobile.DataAccess.Customer/"]

ARG NUGET_PASSWORD
ARG NUGET_USERNAME


RUN dotnet nuget add source https://artifactorycloud.ual.com/artifactory/api/nuget/v3/v-nuget \
            -n artifactory \
            -u ${NUGET_USERNAME}  \
            -p ${NUGET_PASSWORD} \
            --store-password-in-clear-text


COPY . .
WORKDIR "/src/Services/Shopping/ShopProducts/United.Mobile.Services.ShopProducts.Api"

FROM build AS publish
RUN dotnet publish "United.Mobile.Services.ShopProducts.Api.csproj" -c Release --self-contained -r linux-x64 -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "United.Mobile.Services.ShopProducts.Api.dll"]