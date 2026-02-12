# AMWin-RP 
![GitHub release (latest by date including pre-releases)](https://img.shields.io/github/downloads-pre/PKBeam/AMWin-RP/total) ![GitHub release (latest by date including pre-releases)](https://img.shields.io/github/downloads-pre/PKBeam/AMWin-RP/latest/total) &nbsp; ([日本語](https://github.com/PKBeam/AMWin-RP/blob/master/README-JA.md))

A Discord Rich Presence client for Apple Music's native Windows app.  
Also includes scrobbling for Last.FM and ListenBrainz.

<image width=450 src="https://github.com/user-attachments/assets/df5d6a83-4630-4384-b521-bc80c286a499" />
&nbsp; &nbsp; 
<image src=https://github.com/user-attachments/assets/ea63ddf1-d822-4ffd-be9d-24e13701fce9 width=300 />

## Installation
AMWin-RP requires Windows 11 24H2 or later.

Builds can be found [here](https://github.com/PKBeam/AMWin-RP/releases).  

### Which release do I use?
Pick x64 or ARM64 based on what processor your PC has.  
Then there are two files to choose from: the standard one and one marked as `NoRuntime`.

If in doubt, use the unlabelled release (i.e. the one without `NoRuntime`).  
This version works universally, but is larger in size because it bundles the components of .NET that are needed for the app to run.

The `NoRuntime` release is much smaller, but requires you to have the [.NET 10 desktop runtime](https://dotnet.microsoft.com/en-us/download/dotnet/10.0) installed.  
If you don't have this runtime installed, the app will prompt you to do it when it opens.

## Usage
You need the [Microsoft store version](https://apps.microsoft.com/detail/9PFHDD62MXS1) of Apple Music to use AMWin-RP.  

- Open the .exe to start the app.
- AMWin-RP runs in the background, minimised to the system tray.  
- Double clicking on the tray icon brings up the settings window.
  - From here you can adjust individual settings such as run on startup, scrobbling and song detection.  
- The app can be closed by right-clicking on the tray icon and selecting "Exit".  
- By default, the Apple Music app must be open and currently playing music (i.e. not paused) in order for Rich Presence to show.

**Note**: If you use virtual desktops, AMWin-RP and Apple Music must be in the same desktop.  
This is a technical limitation of the UI Automation library used to scrape the Apple Music client app.

## Scrobbling
The scrobbler implementation does not support offline Scrobbles, which means any songs you listen to while not connected to the internet will be lost.

### Last.FM
You will need your own API Key and API Secret from Last.FM.  
To generate one, go to https://www.last.fm/api and select "Get an API Account."  
Enter these in the settings menu with your Last.FM username and password.

The Last.FM password is stored in [Windows Credentials Manager](https://support.microsoft.com/en-us/windows/accessing-credential-manager-1b5c916a-6a16-889f-8581-fc16e8165ac0) under your local Windows account.

### ListenBrainz 
You can scrobble to ListenBrainz by adding your user token in the settings.

## Reporting Bugs
Before creating a new issue, please make sure your problem does not fall under an existing issue.  
If you are reporting a problem, please attach any relevant `.log` files (found in `%localappdata%\AMWin-RichPresence`).

Before posting, please double-check the following:
- The problem isn't already covered by an existing open or closed issue.
- You have RP display enabled in Discord (Settings > Activity Settings > Activity Privacy > Activity Status).
