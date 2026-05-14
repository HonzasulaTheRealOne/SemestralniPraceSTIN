FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY ["BackendAPI.csproj", "./"]
RUN dotnet restore "BackendAPI.csproj"
COPY . .
WORKDIR "/src/"
RUN dotnet build "BackendAPI.csproj" -c Release -o /app/build
RUN dotnet publish "BackendAPI.csproj" -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app
COPY --from=build /app/publish .
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080
ENTRYPOINT ["dotnet", "BackendAPI.dll"]