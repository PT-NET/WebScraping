# Risk Screening API

API REST desarrollada en .NET 10 para realizar búsquedas automatizadas de entidades en listas de alto riesgo (sanciones internacionales, listas de vigilancia y bases de datos relevantes) mediante técnicas de web scraping.

## 📋 Descripción

Este proyecto corresponde al **Ejercicio 1** . La API permite identificar entidades en bases de datos de alto riesgo utilizando web scraping, proporcionando información estructurada y legible para análisis de debida diligencia.

## ✅ Requerimientos Implementados

### Funcionalidades Principales

- ✅ **Web Scraping de múltiples fuentes:**
  - **OFAC** (Office of Foreign Assets Control)
  - **World Bank Debarred Firms**
  - **Offshore Leaks Database**

- ✅ **Búsqueda por nombre de entidad** con retorno de:
  - Número total de coincidencias (hits)
  - Arreglo con elementos encontrados y sus atributos específicos por fuente
  
- ✅ **Validaciones y Controles:**
  - Rate Limiting: Máximo 20 llamadas por minuto por usuario
  - Autenticación JWT con Auth0
  - Manejo robusto de errores con mensajes descriptivos
  - Logs estructurados con Serilog

### Fuentes de Datos y Atributos

#### 1. OFAC (Office of Foreign Assets Control)
- **URL**: https://sanctionssearch.ofac.treas.gov/
- **Atributos extraídos:**
  - Name
  - Address
  - Type
  - Programs
  - List
  - Score

#### 2. World Bank Debarred Firms
- **URL**: https://projects.worldbank.org/en/projects-operations/procurement/debarred-firms
- **Atributos extraídos:**
  - Firm Name
  - Address
  - Country
  - From Date (Ineligibility Period)
  - To Date (Ineligibility Period)
  - Grounds

#### 3. Offshore Leaks Database
- **URL**: https://offshoreleaks.icij.org
- **Atributos extraídos:**
  - Entity
  - Jurisdiction
  - Linked To
  - Data From

## 🏗️ Arquitectura

El proyecto sigue una **Arquitectura Limpia (Clean Architecture)** con separación en capas:

```
WebScraping/
├── API/                          # Capa de Presentación
│   ├── Controllers/              # Controladores REST
│   └── Middleware/               # Middlewares personalizados
├── Application/                  # Capa de Aplicación
│   ├── DTOs/                     # Objetos de Transferencia de Datos
│   ├── Commands/                 # Comandos CQRS
│   └── Validators/               # Validaciones con FluentValidation
├── Domain/                       # Capa de Dominio
│   ├── Entities/                 # Entidades del dominio
│   ├── Interfaces/               # Contratos/Interfaces
│   └── Exceptions/               # Excepciones personalizadas
└── Infrastructure/               # Capa de Infraestructura
    ├── Services/                 # Implementaciones de servicios
    │   └── Scraping/             # Servicios de web scraping
    └── Configuration/            # Configuraciones
```

## 🛠️ Tecnologías Utilizadas

### Framework y Lenguaje
- **.NET 10**
- **C# 14.0**
- **ASP.NET Core Web API**

### Librerías Principales
- **HtmlAgilityPack** - Parsing HTML para web scraping
- **PuppeteerSharp** - Navegador headless para scraping dinámico
- **Selenium WebDriver** - Automatización de navegador
- **Auth0** - Autenticación JWT
- **MediatR** - Patrón Mediator y CQRS
- **FluentValidation** - Validación de entrada
- **AutoMapper** - Mapeo de objetos
- **Serilog** - Logging estructurado
- **Polly** - Políticas de reintentos y resiliencia
- **Swashbuckle** - Documentación OpenAPI/Swagger

## 🚀 Ejecución Local

### Prerrequisitos

1. **.NET 10 SDK**
   ```bash
   # Verificar instalación
   dotnet --version
   ```
   Descargar desde: https://dotnet.microsoft.com/download/dotnet/10.0

2. **IDE recomendado:**
   - Visual Studio 2022 (v17.12 o superior)
   - Visual Studio Code con extensión C#
   - JetBrains Rider

### Configuración

1. **Clonar el repositorio:**
   ```bash
   git clone https://github.com/PT-NET/WebScraping.git
   cd WebScraping
   ```

2. **Configurar Auth0 (Autenticación):**

   Editar `appsettings.json` con tus credenciales de Auth0:
   ```json
   {
     "Auth0": {
       "Domain": "tu-tenant.auth0.com",
       "Audience": "tu-api-identifier"
     }
   }
   ```

   **Obtener credenciales Auth0:**
   - Crear cuenta gratuita en https://auth0.com
   - Crear una API en el Dashboard de Auth0
   - Copiar `Domain` y `Audience`

   En este caso no es necesario configurar 

3. **Restaurar dependencias:**
   ```bash
   dotnet restore
   ```

4. **Compilar el proyecto:**
   ```bash
   dotnet build
   ```

### Ejecución

#### Opción 1: Visual Studio
1. Abrir `WebScraping.sln`
2. Presionar `F5` o hacer clic en **Run**
3. La API estará disponible en: `https://localhost:7172` o `http://localhost:5225`

#### Opción 2: Línea de comandos
```bash
dotnet run --project WebScraping
```

#### Opción 3: Modo Watch (desarrollo)
```bash
dotnet watch run
```

### Verificar funcionamiento

1. **Acceder a Swagger UI:**
   ```
   https://localhost:7172/swagger
   ```

2. **Health Check:**
   ```bash
   curl https://localhost:7172/api/screening/health
   ```

## 📡 Endpoints

### POST /api/screening/screen
Realiza el screening de una entidad en las fuentes seleccionadas.

**Headers requeridos:**
```
Authorization: Bearer {token_jwt}
Content-Type: application/json
```

**Request Body:**
```json
{
  "entityName": "Vladimir Putin",
  "sources": ["OFAC", "WorldBank", "OffshoreLeaks"]
}
```

**Response (200 OK):**
```json
{
  "searchedEntity": "Vladimir Putin",
  "totalHits": 5,
  "searchedAt": "2024-12-03T10:30:00Z",
  "executionTime": "00:00:03.245",
  "hits": [
    {
      "source": "OFAC",
      "entityName": "PUTIN, Vladimir Vladimirovich",
      "attributes": {
        "Name": "PUTIN, Vladimir Vladimirovich",
        "Address": "The Kremlin, Moscow, Russia",
        "Type": "Individual",
        "Programs": "UKRAINE-EO13661",
        "List": "SDN",
        "Score": "100"
      }
    }
  ],
  "errors": []
}
```

**Códigos de Estado:**
- `200 OK` - Búsqueda exitosa
- `400 Bad Request` - Request inválido
- `401 Unauthorized` - Token JWT inválido o ausente
- `429 Too Many Requests` - Rate limit excedido (máx. 20 llamadas/min)
- `500 Internal Server Error` - Error del servidor

**Headers de respuesta:**
- `X-RateLimit-Remaining` - Llamadas restantes en la ventana actual
- `Retry-After` - Segundos a esperar antes de reintentar (en caso de 429)

### GET /api/screening/health
Health check del servicio.

**Response:**
```json
{
  "status": "healthy",
  "timestamp": "2024-12-03T10:30:00Z",
  "version": "1.0.0"
}
```

## 🔐 Autenticación

La API usa **Auth0** para autenticación JWT Bearer.

### Obtener Token de Acceso

#### 1. Usando Auth0 Dashboard (Testing)
```bash
curl --request POST \
  --url https://YOUR_DOMAIN.auth0.com/oauth/token \
  --header 'content-type: application/json' \
  --data '{
    "client_id":"YOUR_CLIENT_ID",
    "client_secret":"YOUR_CLIENT_SECRET",
    "audience":"YOUR_API_IDENTIFIER",
    "grant_type":"client_credentials"
  }'
```

#### 2. Usar token en las peticiones
```bash
curl -X POST https://localhost:7XXX/api/screening/screen \
  -H "Authorization: Bearer YOUR_ACCESS_TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "entityName": "Test Entity",
    "sources": ["OFAC"]
  }'
```

## 📊 Rate Limiting

- **Límite:** 20 llamadas por minuto por usuario autenticado
- **Ventana:** 60 segundos
- **Identificación:** Por `sub` claim del JWT (User ID)
- **Respuesta al exceder:** HTTP 429 con header `Retry-After`

**Configuración en `appsettings.json`:**
```json
{
  "RateLimitSettings": {
    "MaxCallsPerMinute": 20,
    "WindowSizeSeconds": 60
  }
}
```

## 📝 Logging

Los logs se generan con **Serilog** en:
- **Consola** (durante ejecución)
- **Archivos** en carpeta `logs/riskscreening-YYYYMMDD.txt`

**Configuración de niveles:**
```json
{
  "Serilog": {
    "MinimumLevel": {
      "Default": "Information",
      "Override": {
        "Microsoft": "Warning",
        "System": "Warning"
      }
    }
  }
}
```

## 🧪 Testing con Postman

### Importar Colección
1. Descargar colección desde el repositorio: `postman/Risk_Screening_API.postman_collection.json`
2. Importar en Postman
3. Configurar variables de entorno:
   - `base_url`: URL de la API (ej: `https://localhost:7XXX`)
   - `auth0_token`: Token JWT obtenido de Auth0

### Ejemplos de Solicitudes

#### 1. Búsqueda en OFAC
```json
POST {{base_url}}/api/screening/screen
{
  "entityName": "Vladimir Putin",
  "sources": ["OFAC"]
}
```

#### 2. Búsqueda en todas las fuentes
```json
POST {{base_url}}/api/screening/screen
{
  "entityName": "Gazprom",
  "sources": ["OFAC", "WorldBank", "OffshoreLeaks"]
}
```

#### 3. Health Check
```
GET {{base_url}}/api/screening/health
```

## ☁️ Despliegue en Azure (Opcional)

### Opción 1: Azure App Service

#### 1. Crear App Service
```bash
# Login en Azure
az login

# Crear grupo de recursos
az group create --name rg-screening-api --location eastus

# Crear App Service Plan
az appservice plan create \
  --name plan-screening-api \
  --resource-group rg-screening-api \
  --sku B1 \
  --is-linux

# Crear Web App
az webapp create \
  --name screening-api-app \
  --resource-group rg-screening-api \
  --plan plan-screening-api \
  --runtime "DOTNET|10.0"
```

#### 2. Configurar variables de entorno
```bash
az webapp config appsettings set \
  --resource-group rg-screening-api \
  --name screening-api-app \
  --settings \
    Auth0__Domain="your-tenant.auth0.com" \
    Auth0__Audience="your-api-identifier" \
    RateLimitSettings__MaxCallsPerMinute=20
```

#### 3. Desplegar desde GitHub
```bash
az webapp deployment source config \
  --name screening-api-app \
  --resource-group rg-screening-api \
  --repo-url https://github.com/PT-NET/WebScraping \
  --branch main \
  --manual-integration
```

#### 4. Habilitar HTTPS y CORS
```bash
az webapp update \
  --resource-group rg-screening-api \
  --name screening-api-app \
  --https-only true

az webapp cors add \
  --resource-group rg-screening-api \
  --name screening-api-app \
  --allowed-origins "https://yourdomain.com"
```

### Opción 2: Azure Container Apps

#### 1. Crear Dockerfile (si no existe)
```dockerfile
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY ["WebScraping.csproj", "./"]
RUN dotnet restore
COPY . .
RUN dotnet build -c Release -o /app/build

FROM build AS publish
RUN dotnet publish -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "WebScraping.dll"]
```

#### 2. Desplegar en Container Apps
```bash
# Crear Container Registry
az acr create \
  --resource-group rg-screening-api \
  --name screeningapiregistry \
  --sku Basic

# Build y push imagen
az acr build \
  --registry screeningapiregistry \
  --image screening-api:latest .

# Crear Container App
az containerapp create \
  --name screening-api \
  --resource-group rg-screening-api \
  --image screeningapiregistry.azurecr.io/screening-api:latest \
  --target-port 80 \
  --ingress external \
  --env-vars \
    Auth0__Domain="your-tenant.auth0.com" \
    Auth0__Audience="your-api-identifier"
```

### Opción 3: Azure Functions (Serverless)
Para implementación serverless, convertir endpoints a Azure Functions usando isolated worker model de .NET.

## 🔧 Configuración Avanzada

### Modos de Scraping

El proyecto soporta dos modos configurables en `appsettings.json`:

```json
{
  "ScrapingSettings": {
    "ScrapingMode": "DirectScraping",  // o "HeadlessBrowser"
    "TimeoutSeconds": 30,
    "MaxRetries": 3,
    "RetryDelayMs": 1000,
    "UseHeadlessBrowser": true
  }
}
```

- **DirectScraping**: Usa `HttpClient` + `HtmlAgilityPack` (más rápido)
- **HeadlessBrowser**: Usa `PuppeteerSharp` para sitios con JavaScript (más lento pero más robusto)

### CORS

Configurar orígenes permitidos en `Program.cs`:
```csharp
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.WithOrigins(
            "http://localhost:4200",      // Angular Frontend
            "https://yourdomain.com"      // Producción
        )
        .AllowAnyMethod()
        .AllowAnyHeader()
        .WithExposedHeaders("X-RateLimit-Remaining", "Retry-After");
    });
});
```

## 📚 Documentación API

La documentación interactiva está disponible en:
- **Swagger UI**: `https://localhost:7XXX/swagger`
- **OpenAPI JSON**: `https://localhost:7XXX/swagger/v1/swagger.json`

## 🐛 Troubleshooting

### Error: "No se puede conectar a Auth0"
- Verificar configuración en `appsettings.json`
- Confirmar que Auth0 Domain y Audience son correctos
- Revisar logs en `logs/riskscreening-*.txt`

### Error: "Rate limit exceeded"
- Esperar el tiempo indicado en header `Retry-After`
- El límite es 20 llamadas por minuto por usuario

### Error: "Scraping failed"
- Verificar conexión a internet
- Algunos sitios pueden bloquear bots (usar `UseHeadlessBrowser: true`)
- Revisar logs para detalles específicos

### Chrome/Chromium no encontrado (PuppeteerSharp)
```bash
# PuppeteerSharp descarga Chromium automáticamente en el primer uso
# Si falla, descargar manualmente:
dotnet run  # Esperar a que descargue Chromium
```

## 📄 Licencia

Este proyecto fue desarrollado como parte de una prueba técnica para demostración de habilidades en .NET y web scraping.

## 👤 Autor

Diego Flores. Desarrollado como **Ejercicio 1**.



---

**Nota**: Este README corresponde al **Ejercicio 1** (Web Scraping API). El **Ejercicio 2** (Aplicación Web con Angular/React) se encuentra en un repositorio separado.
