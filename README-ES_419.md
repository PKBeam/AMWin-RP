# AMWin-RP 
![GitHub release (latest by date including pre-releases)](https://img.shields.io/github/downloads-pre/PKBeam/AMWin-RP/total) ![GitHub release (latest by date including pre-releases)](https://img.shields.io/github/downloads-pre/PKBeam/AMWin-RP/latest/total) &nbsp; ([한국어](README-KO.md) | [日本語](README-JA.md) | [Russian](README-RU.md) | [Español de España](README-ES.md) | [Deutsch](README-DE.md))

Un complemento de presencia enriquecida de Discord (Discord Rich Presence) para la aplicación nativa de Apple Music en Windows.
También incluyen servicios de *scrobbling* para Last.FM y ListenBrainz.

<image width=450 src="https://github.com/user-attachments/assets/d26a1318-2579-493a-a720-b51408d6178b" />
&nbsp; &nbsp; 
<image src=https://github.com/user-attachments/assets/16fbb938-4edd-4968-a8d9-aa7922ea7ee6 width=400 />

## Requisitos
AMWin-RP requiere Windows 11 24H2 o una versión posterior.

Las versiones se pueden encontrar [aquí](https://github.com/PKBeam/AMWin-RP/releases).

### ¿Qué versión debo utilizar?
Elija x64 o ARM64 según el tipo de procesador que tenga su computadora.
Luego, hay dos archivos entre los que elegir: el normal y uno marcado como *NoRuntime*.

Si no estás seguro, usa la versión normal (es decir, la que no está marcada como *NoRuntime*).
Esta versión funciona en todos los sistemas, sin embargo, ocupa más espacio de almacenamiento porque incluye los componentes de .NET necesarios para que la aplicación funcione.

La versión *NoRuntime* es mucho más ligera, pero requiere que tengas instalado el [Entorno de ejecución de escritorio de .NET 10](https://dotnet.microsoft.com/en-us/download/dotnet/10.0).
Si no tienes este entorno de ejecución instalado, la aplicación te pedirá que lo hagas cuando se abra.

## ¿Cómo usar?
Necesitas tener la [versión de Apple Music de Microsoft Store](https://apps.microsoft.com/detail/9PFHDD62MXS1) para que AMWin-RP funcione.

- Haz clic en el archivo .exe para iniciar la aplicación.
- AMWin-RP se ejecutará en segundo plano, minimizado en la barra de tareas.
- Al hacer doble clic en el ícono de la barra de tareas, se abrirá la ventana de configuración.
  
- Desde aquí puedes personalizar las configuraciones individuales, por ejemplo, si quieres que se abra al iniciar el sistema, la función de *scrobbling* y la detección de música. 
- Puedes cerrar la aplicación haciendo clic con el botón derecho en el ícono de la barra de tareas y seleccionando "Salir".

**Nota**: Si usas escritorios virtuales, AMWin-RP y Apple Music deben estar en el mismo escritorio. 
Esta es una limitación técnica de la biblioteca UI Automation utilizada para extraer datos de la aplicación cliente de Apple Music.

## Scrobbling
La implementación de *scrobbling* no admite el *scrobbling* sin conexión, lo que significa que cualquier música que reproduzcas sin conexión a Internet, no se registrará.

### Last.FM
Necesitarás tu propia clave API y tu secreto API de Last.FM.
Para generarlos, ve a https://www.last.fm/es/api y selecciona "*Get an API account*".
Introdúcelos en la configuración de Last.FM junto con tu nombre de usuario y contraseña de Last.FM.

La contraseña de Last.FM se guarda en el [Administrador de credenciales de Windows](https://support.microsoft.com/es-es/windows/administrador-de-credenciales-en-windows-1b5c916a-6a16-889f-8581-fc16e8165ac0) en tu cuenta local de Windows.
### ListenBrainz 
Puedes hacer *scrobbling* en ListenBrainz agregando tu token de usuario en la configuración de ListenBrainz.
## Cómo informar de errores
Antes de crear un nuevo informe (*Issue*), asegúrate de que tu problema no se trate ya en algún informe existente.
Si vas a enviar un informe, adjunta cualquier archivo `.log` relevante (se encuentran en `%localappdata%\AMWin-RichPresence`). Y, si es posible, escriba el informe en inglés.

Antes de enviarlo, revisa lo siguiente:
- Que el problema no esté ya cubierto por un informe abierto o cerrado.
- Que tengas "Compartir mi actividad" activado en Discord (Ajustes de usuario > Actividad > Privacidad de la actividad > Compartiendo actividad).
