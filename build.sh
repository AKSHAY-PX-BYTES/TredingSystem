#!/bin/bash
# Reads FRONTEND value from frontend.config and builds accordingly
set -e

FRONTEND=$(grep 'FRONTEND=' frontend.config | cut -d'=' -f2 | tr -d '[:space:]')
echo "============================================"
echo "  FRONTEND = $FRONTEND"
echo "============================================"

rm -rf build_output
mkdir -p build_output

if [ "$FRONTEND" = "angular" ]; then
  echo ">>> Building Angular..."
  cd src/TradingSystem.Angular
  npm ci
  npm run build -- --configuration=production
  cd ../..
  cp -r src/TradingSystem.Angular/dist/trading-system/browser/* build_output/
  echo ">>> Angular build complete"

elif [ "$FRONTEND" = "blazor" ]; then
  echo ">>> Building Blazor..."
  dotnet publish src/TradingSystem.Web/TradingSystem.Web.csproj -c Release -o output
  cp -r output/wwwroot/* build_output/
  echo ">>> Blazor build complete"

else
  echo ">>> ERROR: Invalid FRONTEND='$FRONTEND'. Set 'angular' or 'blazor' in frontend.config"
  exit 1
fi

echo ">>> Build output ready in build_output/"
