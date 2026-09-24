# Tie Alert Bot (C#)

Requisitos: .NET 10 SDK para ejecución local. GitHub Actions instala el SDK automáticamente.

## GitHub Actions

El workflow `.github/workflows/monitor-citas.yml` busca citas solo en las franjas en las que se liberan (hora de Madrid):

- 06:00 – 07:00
- 08:45 – 09:15
- 09:45 – 10:15

Arranca unos 25 minutos antes de cada bloque, espera a que empiece la franja y, dentro de ella, consulta cada 30 segundos (`INTERVAL_SECONDS`). Al terminar la última franja del bloque, se detiene. El cambio de horario de verano/invierno se gestiona solo. También se puede lanzar a mano desde la pestaña **Actions** (fuera de franja termina en seguida). No necesita un archivo `.env`.

### Crear los secrets (credenciales)

1. Abre directamente <https://github.com/Patry-vll/Tie-alert-chat/settings/secrets/actions>.
   Para llegar a mano: en el repositorio pulsa **Settings** (Configuración) en la barra superior. En el menú de la izquierda, baja hasta la sección **Security** (Seguridad), despliega **Secrets and variables** (Secretos y variables) y elige **Actions**. Esta opción no aparece junto a Ramas, Conjuntos de reglas, Copilot o Entornos, sino más abajo. En una ventana estrecha, el menú de la izquierda aparece plegado encima del contenido.
2. En la pestaña **Secrets** pulsa **New repository secret** (Nuevo secreto del repositorio). No uses la pestaña *Variables*, que no está cifrada.
3. Crea el primero:
   - **Name:** `BOT_TOKEN`
   - **Secret:** el token que te dio @BotFather (con el formato `123456789:ABC...`).
   - Pulsa **Add secret**.
4. Repite con:
   - **Name:** `CHAT_ID`
   - **Secret:** el identificador numérico de tu chat. Para obtenerlo, escribe cualquier mensaje a tu bot en Telegram y abre en tu navegador `https://api.telegram.org/bot<TU_TOKEN>/getUpdates`. El número está en `"chat":{"id": ...}` (en grupos es negativo, por ejemplo `-100...`).
5. Comprueba que aparecen los dos en la lista. Una vez guardados, GitHub no vuelve a mostrar su valor; solo se pueden sustituir.
6. Ve a **Actions**, elige **Monitor de citas TIE** y pulsa **Run workflow** para probarlo.

Para ver **Settings** hay que ser propietario o administrador del repositorio. El workflow inyecta los secrets como variables de entorno solo durante la ejecución, y GitHub los tapa con `***` si aparecen en los logs. No los pongas en el YAML, el código ni el historial de Git.

**Límites de GitHub Actions:**

- Cada día se usan unos 200 minutos (dos bloques de hasta ~2 h), unos 6.000 al mes. En un repositorio **privado** del plan gratuito solo hay 2.000 minutos al mes. Para que salga gratis, el repositorio debe ser **público**: los minutos no tienen límite y los secrets siguen ocultos.
- GitHub puede retrasar las ejecuciones programadas, sobre todo en horas punta. Por eso cada bloque tiene un disparo de respaldo 15 minutos después.
- En repositorios públicos, GitHub desactiva los workflows programados tras 60 días sin actividad en el repositorio (avisa antes por correo). Si ocurre, basta con reactivarlo desde **Actions**.

El estado de las citas ya notificadas se conserva mediante GitHub Actions Cache para evitar repetir avisos entre ejecuciones. Puedes borrar el cache desde **Actions → Caches** para reiniciar el historial.

## Ejecución local

```powershell
$env:BOT_TOKEN = "token_del_bot"
$env:CHAT_ID = "identificador_del_chat"
$env:RUN_ONCE = "true"
dotnet run --project src/TieAlertBot.csproj
```

Sin `RUN_ONCE` y con `SCHEDULE_WINDOWS` (por ejemplo `"06:00-07:00,08:45-09:15"`) busca solo en esas franjas de la zona `TIME_ZONE` (por defecto `Europe/Madrid`). Sin ninguna de las dos, vigila en bucle continuo. `INTERVAL_SECONDS` fija el intervalo (30 a 3600 segundos).

**Limitación:** ICP+ puede requerir seleccionar provincia/trámite y recorrer formularios asociados a una sesión. El parser actual solo examina filas HTML de la página descargada, por lo que el workflow automatiza la ejecución y el aviso, pero no garantiza que detecte citas reales. Confirma siempre una cita en la web oficial.
