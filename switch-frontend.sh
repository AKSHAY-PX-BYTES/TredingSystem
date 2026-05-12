#!/bin/bash
# Reads FRONTEND value from frontend.config and copies the correct netlify.toml

CONFIG_FILE="frontend.config"
FRONTEND=$(grep -oP '(?<=FRONTEND=)\w+' "$CONFIG_FILE")

echo ">>> Frontend selected: $FRONTEND"

if [ "$FRONTEND" = "angular" ]; then
  cp src/TradingSystem.Angular/netlify.toml netlify.toml
  echo ">>> Activated Angular configuration"
elif [ "$FRONTEND" = "blazor" ]; then
  cp src/TradingSystem.Web/netlify.toml netlify.toml
  echo ">>> Activated Blazor configuration"
else
  echo ">>> ERROR: Invalid FRONTEND value '$FRONTEND'. Use 'angular' or 'blazor'"
  exit 1
fi

echo ">>> Done. Now commit and push netlify.toml to deploy."
