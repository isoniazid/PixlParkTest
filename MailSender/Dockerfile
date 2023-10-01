FROM mcr.microsoft.com/dotnet/aspnet:7.0 AS base
WORKDIR /app
EXPOSE 80

FROM mcr.microsoft.com/dotnet/sdk:7.0 AS build
WORKDIR /src
COPY ["MailSender.csproj", "."]
RUN dotnet restore "./MailSender.csproj"
COPY . .
WORKDIR "/src/."
RUN dotnet build "MailSender.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "MailSender.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "MailSender.dll"]