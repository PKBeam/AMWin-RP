# AMWin-RP
A Discord Rich Presence client for Apple Music's native Windows app.  
Last.FM scrobbling is also supported!

![GitHub release (latest by date including pre-releases)](https://img.shields.io/github/downloads-pre/PKBeam/AMWin-RP/total) ![GitHub release (latest by date including pre-releases)](https://img.shields.io/github/downloads-pre/PKBeam/AMWin-RP/latest/total) 

![image](https://user-images.githubusercontent.com/18737124/236110561-e11eabf5-d2c4-4fb3-a743-3152a1aef916.png)

![image](https://user-images.githubusercontent.com/18737124/213862194-e02ec9e7-07ab-481f-9dc5-451b9159c903.png)

## Installation

Releases can be found [here](https://github.com/PKBeam/AMWin-RP/releases).

If you have (or are able to install) the [.NET 7.0 runtime](https://dotnet.microsoft.com/en-us/download/dotnet/7.0), you can download the `NoRuntime` release.

Otherwise, download the other release (the one that isn't labelled as `NoRuntime`).  
This release is larger in size as it bundles the components of .NET 7.0 that are needed for the app to run.

## Usage
Only the [Microsoft store version](https://apps.microsoft.com/store/detail/apple-music-preview/9PFHDD62MXS1) of Apple Music is supported.  
There's no support for iTunes, Apple Music via WSA, or any third-party players.

The app runs in the background, minimised to the system tray. It can be closed by right-clicking on the tray icon and selecting "Exit".  
In order for the rich presence to show, the Apple Music app must be open and currently playing music (i.e. not paused).  

If you like, you can set the app to automatically run when Windows starts. You can do this by double clicking on the tray icon and changing the settings.  

<hr/>

## Last.FM Scrobbling
Scrobbling to Last.FM is supported.  You will need your own Last.FM API Key and Key Secret.  To generate one, go to https://www.last.fm/api and select "Get an API Account."  Use these two pieces of information along with your Last.FM username and password:

![AMWin-RP-Scrobbler_Settings](https://user-images.githubusercontent.com/317772/215867741-2999591c-35eb-442a-a349-b8e9046634fb.png)

The Last.FM password is stored in [Windows Credentials Manager](https://support.microsoft.com/en-us/windows/accessing-credential-manager-1b5c916a-6a16-889f-8581-fc16e8165ac0) under your local Windows account.

This Scrobbler implementation does not support offline Scrobbles, which means any songs you listen to while not connected to the Internet will be lost.

<hr/>


## How does it work?

**(Technical details ahead)**

The biggest challenge here is being able to extract song information out of the Apple Music app.

This is achieved using .NET's [UI Automation](https://learn.microsoft.com/en-us/dotnet/framework/ui-automation/ui-automation-overview), which lets us access UI elements of any window on the user's desktop.

The general process is this:
- We look for the Apple Music window on the desktop.
- We then navigate to known UI controls that hold the info we're after (e.g. song name).
- We extract this information and send it to the part of the program that handles the Discord RPC.

The other problem is getting the song's cover art.  

(It's not well documented, but Discord RPC now lets you specify arbitrary images by sending the image URL in place of the assets key.)  

We can't use UI Automation to get the image being displayed in the window (as far as I know). Instead we send an HTTP request to the Apple Music website, where we try to search for the song and grab the cover image URL from there. It's not ideal but gives us what we're looking for most of the time.


