# Stage 1: Build Blazor WASM
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["TradingSystem.Web.csproj", "."]
RUN dotnet restore
COPY . .
RUN dotnet publish -c Release -o /app/publish
RUN ls -la /app/publish/ && ls -la /app/publish/wwwroot/ || true

# Stage 2: Serve with nginx
FROM nginx:alpine
COPY --from=build /app/publish/wwwroot /usr/share/nginx/html
COPY nginx.conf /etc/nginx/nginx.conf
RUN ls -la /usr/share/nginx/html/
EXPOSE 10000
CMD ["nginx", "-g", "daemon off;"]
