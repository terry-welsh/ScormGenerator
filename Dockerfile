FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY ScormGenerator.slnx .
COPY src/ src/
RUN dotnet restore src/ScormGen.Web/ScormGen.Web.csproj
RUN dotnet publish src/ScormGen.Web/ScormGen.Web.csproj \
    -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080
ENTRYPOINT ["dotnet", "ScormGen.Web.dll"]
