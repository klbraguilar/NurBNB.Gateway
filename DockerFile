FROM mcr.microsoft.com/dotnet/aspnet:6.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
WORKDIR /src
COPY ["NurBNB.Gateway.Web/NurBNB.Gateway.Web.csproj", "NurBNB.Gateway.Web/"]
RUN dotnet restore "NurBNB.Gateway.Web/NurBNB.Gateway.Web.csproj"
COPY . .
WORKDIR "/src/NurBNB.Gateway.Web"
RUN dotnet build "NurBNB.Gateway.Web.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "NurBNB.Gateway.Web.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "NurBNB.Gateway.Web.dll"]
