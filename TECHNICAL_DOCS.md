## How does it work?
The biggest challenge here is being able to extract song information out of the Apple Music app.

This is achieved using Microsoft's [UI Automation API](https://learn.microsoft.com/en-us/windows/win32/winauto/windows-automation-api-overview) via the FlaUI library, which lets us access UI elements of any window on the user's desktop.

The general process is this:
- We look for the AppleMusic.exe process.
- We get the window belonging to that process.
- We then navigate to known UI controls that hold the info we're after (e.g. song name).
- We extract this information and send it to the part of the program that handles the Discord RPC.

The other problem is getting the song's cover art.  

(It's not well documented, but Discord RPC now lets you specify arbitrary images by sending the image URL in place of the assets key.)  

We can't use UI Automation to get the image being displayed in the window (as far as I know). Instead we send an HTTP request to the Last.FM or Apple Music website, where we try to search for the song and grab the cover image URL from there.  
It's not ideal but gives us what we're looking for most of the time.
