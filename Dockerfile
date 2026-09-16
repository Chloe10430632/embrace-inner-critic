FROM node:22-alpine AS frontend-build
WORKDIR /src/frontend
COPY frontend/package.json frontend/package-lock.json ./
RUN npm ci
COPY frontend/ ./
ENV NUXT_PUBLIC_API_BASE=""
RUN npm run generate

FROM mcr.microsoft.com/dotnet/sdk:10.0-alpine AS backend-build
WORKDIR /src
COPY backend/EmbraceInnerCritic.Api/EmbraceInnerCritic.Api.csproj backend/EmbraceInnerCritic.Api/
RUN dotnet restore backend/EmbraceInnerCritic.Api/EmbraceInnerCritic.Api.csproj
COPY backend/EmbraceInnerCritic.Api/ backend/EmbraceInnerCritic.Api/
RUN dotnet publish backend/EmbraceInnerCritic.Api/EmbraceInnerCritic.Api.csproj \
    --configuration Release \
    --no-restore \
    --output /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:10.0-alpine AS final
WORKDIR /app
COPY --from=backend-build /app/publish ./
COPY --from=frontend-build /src/frontend/.output/public ./wwwroot
ENV ASPNETCORE_ENVIRONMENT=Production
EXPOSE 10000
ENTRYPOINT ["sh", "-c", "dotnet EmbraceInnerCritic.Api.dll --urls http://0.0.0.0:${PORT:-10000}"]
