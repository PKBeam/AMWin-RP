# AMWin-RP 
![GitHub release (latest by date including pre-releases)](https://img.shields.io/github/downloads-pre/PKBeam/AMWin-RP/total) ![GitHub release (latest by date including pre-releases)](https://img.shields.io/github/downloads-pre/PKBeam/AMWin-RP/latest/total) &nbsp; ([한국어](README-KO.md) | [日本語](README-JA.md) | [Russian](README-RU.md) | [Español](README-ES.md) | [Deutsch](README-DE.md))

Ein Discord-Rich-Presence-Client fuer die native Windows-App von Apple Music.  
Enthaelt ausserdem Scrobbling fuer Last.FM und ListenBrainz.

<image width=450 src="https://github.com/user-attachments/assets/df5d6a83-4630-4384-b521-bc80c286a499" />
&nbsp; &nbsp; 
<image src=https://github.com/user-attachments/assets/ea63ddf1-d822-4ffd-be9d-24e13701fce9 width=300 />

## Installation
AMWin-RP erfordert Windows 11 24H2 oder neuer.

Builds findest du [hier](https://github.com/PKBeam/AMWin-RP/releases).  

### Welche Version soll ich verwenden?
Waehle x64 oder ARM64, je nachdem, welchen Prozessor dein PC hat.  
Danach hast du zwei Dateien zur Auswahl: die Standardversion und eine mit der Kennzeichnung `NoRuntime`.

Wenn du dir unsicher bist, nutze die nicht gekennzeichnete Version (also die ohne `NoRuntime`).  
Diese Version funktioniert universell, ist aber groesser, weil sie die fuer die App benoetigten .NET-Komponenten bereits mitliefert.

Die `NoRuntime`-Version ist deutlich kleiner, setzt aber voraus, dass die [.NET 10 Desktop Runtime](https://dotnet.microsoft.com/en-us/download/dotnet/10.0) installiert ist.  
Falls diese Runtime nicht installiert ist, fordert dich die App beim Start dazu auf.

## Verwendung
Um AMWin-RP zu nutzen, brauchst du die [Microsoft-Store-Version](https://apps.microsoft.com/detail/9PFHDD62MXS1) von Apple Music.  

- Starte die App, indem du die `.exe` oeffnest.
- AMWin-RP laeuft im Hintergrund und wird im Infobereich (System Tray) minimiert.  
- Ein Doppelklick auf das Tray-Icon oeffnet das Einstellungsfenster.
  - Dort kannst du einzelne Optionen wie Autostart, Scrobbling und Song-Erkennung anpassen.  
- Die App kann geschlossen werden, indem du mit der rechten Maustaste auf das Tray-Icon klickst und "Exit" auswaehlst.  
- Standardmaessig muss die Apple-Music-App geoeffnet sein und gerade Musik abspielen (also nicht pausiert), damit Rich Presence angezeigt wird.

**Hinweis**: Wenn du virtuelle Desktops verwendest, muessen AMWin-RP und Apple Music auf demselben Desktop sein.  
Das ist eine technische Einschraenkung der UI-Automation-Bibliothek, die zum Auslesen der Apple-Music-Client-App verwendet wird.

## Scrobbling
Die Scrobbler-Implementierung unterstuetzt keine Offline-Scrobbles. Das bedeutet, dass Songs, die du ohne Internetverbindung hoerst, verloren gehen.

### Last.FM
Du brauchst einen eigenen API Key und ein API Secret von Last.FM.  
Um diese zu erstellen, gehe auf https://www.last.fm/api und waehle "Get an API Account."  
Trage die Daten anschliessend zusammen mit deinem Last.FM-Benutzernamen und Passwort in den Einstellungen ein.

Das Last.FM-Passwort wird im [Windows-Anmeldeinformations-Manager](https://support.microsoft.com/en-us/windows/accessing-credential-manager-1b5c916a-6a16-889f-8581-fc16e8165ac0) unter deinem lokalen Windows-Konto gespeichert.

### ListenBrainz 
Du kannst zu ListenBrainz scrobbeln, indem du in den Einstellungen dein Benutzer-Token hinterlegst.

## Fehler melden
Bevor du ein neues Issue erstellst, pruefe bitte, ob dein Problem bereits in einem bestehenden Issue behandelt wird.  
Wenn du ein Problem meldest, fuege bitte alle relevanten `.log`-Dateien bei (zu finden unter `%localappdata%\AMWin-RichPresence`).

Bitte pruefe vor dem Posten ausserdem Folgendes:
- Das Problem ist nicht bereits in einem offenen oder geschlossenen Issue enthalten.
- Du hast die RP-Anzeige in Discord aktiviert (Settings > Activity Settings > Activity Privacy > Activity Status).
