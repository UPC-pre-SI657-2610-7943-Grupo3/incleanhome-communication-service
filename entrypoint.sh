#!/bin/sh
# ============================================================================
# entrypoint.sh — materializa el JSON de Firebase (que viene como env var
# desde Azure Key Vault) como archivo en /app antes de arrancar .NET.
#
# Diseño: seguimos 12-factor apps y buenas prácticas de secretos:
#   - El secreto vive en Key Vault (fuente de verdad, auditable, rotable)
#   - El Bicep lo expone al contenedor como env var (FIREBASE_SERVICE_ACCOUNT_JSON)
#   - Este script lo materializa como archivo justo antes del arranque,
#     porque el SDK de Firebase Admin espera un path a archivo
#   - La imagen Docker queda LIMPIA (sin secretos horneados), portable
#     entre ambientes, y rotar la credencial es cambiar el Bicep
# ============================================================================
set -eu

CREDS_FILE="/app/firebase-service-account.json"

if [ -n "${FIREBASE_SERVICE_ACCOUNT_JSON:-}" ]; then
    echo "$FIREBASE_SERVICE_ACCOUNT_JSON" > "$CREDS_FILE"
    chmod 400 "$CREDS_FILE"
    echo "[entrypoint] Firebase credentials materialized at $CREDS_FILE"
else
    echo "[entrypoint] WARN: FIREBASE_SERVICE_ACCOUNT_JSON not set — push notifications will be disabled."
fi

exec dotnet InCleanHome.CommunicationService.dll