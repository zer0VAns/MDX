# 1. Etapa de compilación (SDK de .NET 10)
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copiar archivos de proyecto para restaurar dependencias
COPY ["mdxecommerce/mdxecommerce.csproj", "mdxecommerce/"]
COPY ["mdxecommerce.Client/mdxecommerce.Client.csproj", "mdxecommerce.Client/"]
RUN dotnet restore "mdxecommerce/mdxecommerce.csproj"

# Copiar el resto del código y compilar
COPY . .
WORKDIR "/src/mdxecommerce"
RUN dotnet build "mdxecommerce.csproj" -c Release -o /app/build

# 2. Etapa de publicación
FROM build AS publish
RUN dotnet publish "mdxecommerce.csproj" -c Release -o /app/publish /p:UseAppHost=false

# 3. Etapa final de ejecución (Runtime de ASP.NET Core 10)
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
COPY --from=publish /app/publish .

# Configurar el puerto requerido por la capa gratuita de Render
ENV ASPNETCORE_URLS=http://+:10000
EXPOSE 10000

ENTRYPOINT ["dotnet", "mdxecommerce.dll"]
 