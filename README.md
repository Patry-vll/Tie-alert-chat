# Tie Alert Bot (C#)

Requisitos: .NET 10 SDK para ejecución local. GitHub Actions instala el SDK automáticamente.

## GitHub Actions

El workflow `.github/workflows/monitor-citas.yml` se ejecuta cada 15 minutos y también se puede iniciar manualmente desde la pestaña **Actions** del repositorio. No necesita un archivo `.env`.

Añade estos dos **repository secrets** en GitHub: **Settings → Secrets and variables → Actions → New repository secret**:

- `BOT_TOKEN`: token vigente del bot de Telegram.
- `CHAT_ID`: identificador numérico del chat destino. Escribe primero al bot para iniciar el chat; en un grupo, añade el bot y envía un mensaje al grupo.

El workflow los inyecta como variables de entorno únicamente durante la ejecución. No los pongas en el YAML, el código ni en el historial de Git. Los secrets deben pertenecer al mismo repositorio donde está el workflow; los workflows de `pull_request` desde forks no reciben esos secrets.

El estado de las citas ya notificadas se conserva mediante GitHub Actions Cache para evitar repetir avisos entre ejecuciones. Puedes borrar el cache desde **Actions → Caches** para reiniciar el historial.

## Ejecución local

```powershell
$env:BOT_TOKEN = "token_del_bot"
$env:CHAT_ID = "identificador_del_chat"
$env:RUN_ONCE = "true"
dotnet run --project src/TieAlertBot.csproj
```

Para dejarlo vigilando en bucle, omite `RUN_ONCE`; `INTERVAL_SECONDS` permite configurar el intervalo local (30 a 3600 segundos).

**Limitación:** ICP+ puede requerir seleccionar provincia/trámite y recorrer formularios asociados a una sesión. El parser actual solo examina filas HTML de la página descargada, por lo que el workflow automatiza la ejecución y el aviso, pero no garantiza que detecte citas reales. Confirma siempre una cita en la web oficial.
