FROM mcr.microsoft.com/dotnet/aspnet:8.0-nanoserver-ltsc2022 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:8.0-nanoserver-ltsc2022 AS build
WORKDIR /src
COPY ["ProjectorRaysWeb/ProjectorRaysWeb.csproj", "ProjectorRaysWeb/"]
RUN dotnet restore "ProjectorRaysWeb/ProjectorRaysWeb.csproj"
# This will copy everything, including the executables folder
COPY . . 
WORKDIR "/src/ProjectorRaysWeb"
RUN dotnet build "ProjectorRaysWeb.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "ProjectorRaysWeb.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .

# Copy the executables folder into the final image
COPY ["ProjectorRaysWeb/executables", "/app/executables/"]

ENTRYPOINT ["dotnet", "ProjectorRaysWeb.dll"]
