# 1. Etapa de compilación (SDK de .NET 10)
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copiar archivos de proyecto usando el nombre exacto de tus carpetas (con E mayúscula)
COPY ["mdxEcommerce/mdxEcommerce.csproj", "mdxEcommerce/"]
COPY ["mdxEcommerce.Client/mdxEcommerce.Client.csproj", "mdxEcommerce.Client/"]
RUN dotnet restore "mdxEcommerce/mdxEcommerce.csproj"

# Copiar el resto del código y compilar
COPY . .
WORKDIR "/src/mdxEcommerce"
RUN dotnet build "mdxEcommerce.csproj" -c Release -o /app/build

# 2. Etapa de publicación
FROM build AS publish
RUN dotnet publish "mdxEcommerce.csproj" -c Release -o /app/publish /p:UseAppHost=false

# 3. Etapa final de ejecución (Runtime de ASP.NET Core 10)
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
COPY --from=publish /app/publish .

# Configurar el puerto dinámico de Render
ENV ASPNETCORE_URLS=http://+:10000
EXPOSE 10000

# Nombre exacto de tu DLL compilada
ENTRYPOINT ["dotnet", "mdxEcommerce.dll"]
