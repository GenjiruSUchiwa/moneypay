FROM mcr.microsoft.com/dotnet/sdk:10.0-alpine AS build
WORKDIR /src
ENV HUSKY=0

COPY Directory.Build.props Directory.Packages.props ./
COPY src/Directory.Build.props src/Directory.Build.props
COPY src/MoniPay.Api/MoniPay.Api.csproj src/MoniPay.Api/MoniPay.Api.csproj
COPY src/MoniPay.Data/MoniPay.Data.csproj src/MoniPay.Data/MoniPay.Data.csproj
COPY src/MoniPay.Kernel/MoniPay.Kernel.csproj src/MoniPay.Kernel/MoniPay.Kernel.csproj
COPY src/MoniPay.Sessions/MoniPay.Sessions.csproj src/MoniPay.Sessions/MoniPay.Sessions.csproj
COPY src/MoniPay.Users/MoniPay.Users.csproj src/MoniPay.Users/MoniPay.Users.csproj
COPY src/MoniPay.Wallet/MoniPay.Wallet.csproj src/MoniPay.Wallet/MoniPay.Wallet.csproj
RUN dotnet restore src/MoniPay.Api/MoniPay.Api.csproj

COPY src/ src/
RUN dotnet publish src/MoniPay.Api/MoniPay.Api.csproj -c Release -o /app/publish \
    --no-restore -p:UseAppHost=false -p:MinVerSkip=true

FROM mcr.microsoft.com/dotnet/aspnet:10.0-alpine AS runtime
WORKDIR /app
# Alpine ships no IANA database, and a CEMAC timestamp is stored and rendered in Africa/Douala.
RUN apk add --no-cache tzdata \
    && addgroup -S monipay && adduser -S monipay -G monipay
COPY --from=build /app/publish .
USER monipay

ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080
HEALTHCHECK --interval=30s --timeout=3s --start-period=10s \
    CMD wget -qO- http://127.0.0.1:8080/health || exit 1

ENTRYPOINT ["dotnet", "MoniPay.Api.dll"]
