# AMWin-RP 
![GitHub release (latest by date including pre-releases)](https://img.shields.io/github/downloads-pre/PKBeam/AMWin-RP/total) ![GitHub release (latest by date including pre-releases)](https://img.shields.io/github/downloads-pre/PKBeam/AMWin-RP/latest/total) &nbsp; ([English](README.md) | [한국어](README-KO.md) | [日本語](README-JA.md) | [Russian](README-RU.md))

Un cliente de Discord Rich Presence para la app nativa de Apple Music en Windows.
Además incluye scrobbling para Last.FM y ListenBrainz.

<image width=450 src="https://github.com/user-attachments/assets/7a8e738a-d7af-4a67-9cf4-f4cf9c3c31d1" />
&nbsp; &nbsp; 
<image src=https://github.com/user-attachments/assets/f5464285-77de-4f98-ac8c-38dca5991c7f width=300 />

## Instalación
AMWin-RP requiere de Windows 11 24H2 o posterior.

Las builds se pueden encontrar en [here](https://github.com/PKBeam/AMWin-RP/releases).  

### ¿Qué versión debo usar?
Elije x64 o ARM64 en función del procesador de tu PC.
Después hay dos archivos entre los que elegir: el normal y el marcado como `NoRuntime`.

En caso de duda, usa el *"release"* sin marcar (el que no contiene `NoRuntime`).
Esta versión funciona universalmente, pero es de mayor tamaño debido a que tiene empaquetados los componentes de .NET necesarios para ejecutar la app.

La versión `NoRuntime` tiene un tamaño notablemente más reducido, pero requiere tener instalado [.NET 10 desktop runtime](https://dotnet.microsoft.com/en-us/download/dotnet/10.0).
Si no tienes el *runtime* instalado, el programa te notificará que lo hagas cuando se abra.

## Uso
Necesitas la versión de Apple Music de la [Microsoft store](https://apps.microsoft.com/detail/9PFHDD62MXS1) para poder usar AMWin-RP.

- Ejecuta el .exe para iniciar la app.
- AMWin-RP se ejecuta en segundo plano, minimizado en la bandeja del sistema.
- Hacer doble clic en el icono de la bandeja abrirá la ventana de ajustes.
  - Aquí podrás configurar las distintas opciones como ejecutar al iniciar el equipo, scrobbling y detección de canciones.
- La aplicación puede cerrarse con clic derecho sobre el icono de la bandeja y seleccionando "Salir".
- Por defecto, la app de Apple Music debe estar abierta y reproduciendo música (no pausada) para que se muestre el Rich Presence.

**Nota**: Si haces uso de escritorios virtuales, AMWin-RP y Apple Music deberán estar en el mismo escritorio.
Esta es una limitación técnica de la libresría UI Automation usada para extraer datos del cliente de Apple Music.

## Scrobbling
La implementación del scrobbler no admite los Scrobbles sin conexión, lo que implica que cualquier canción escuchada sin estar conectado a internét no será registrada.

### Last.FM
Necesitarás tu propia Clave API y API Secret de Last.FM.
Para generar una, entra en https://www.last.fm/api y selecciona "Get an API Account".
Introdúcelas en el menú de ajustes con tu nombre de usuario de Last.FM y tu contraseña.

La contraseña de Last.FM se amacena en el [Administrador de credenciales de Windows](https://support.microsoft.com/es-es/windows/administrador-de-credenciales-en-windows-1b5c916a-6a16-889f-8581-fc16e8165ac0) bajo tu cuenta local de Windows. 

### ListenBrainz 
Puedes scrobblear en ListenBrainz añadiendo tu token de usuario en ajustes.

## Reportar Errores
Antes de crear un nuevo issue, asegúrate de que el problema no esté incluído en un issue ya existente.
Si estás reportando un problema, por favor adjunta cualquier archivo `.log.` de relevancia (ubicados en `%localappdata%\AMWin-RichPresence`). 

Antes de publicar, comprueba lo siguiente:
- El problema no ha sido cubierto en ningún issue ya sea abierto o cerrado.
- Tienes activado mostrar RP en Discord (Ajustes > Ajustes de Actividad > Privacidad de la actividad > Compartir mi Actividad).
