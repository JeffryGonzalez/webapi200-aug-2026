# Updating Classroom Sync

The version I had installed wasn't the correct version. Sorry. This will step you through updating to the correct version.

## Uninstall the current version in VS Code

In Visual Studio Code, select the Extensions icon on the left side of the screen (looks like some blocks).

Under "Installed" in the extensions view, find 'Classroom Sync'. Click on it, and click the Uninstall button on the page that gets displayed.

## Close VS Code and reopen it.

It gets confused sometimes. This will help.

## Download the correct version of the extension

Open the link shared by your instructor. It will download the new version in your Downloads folder.

## Back in VS Code - install the new version

Back on the extensions tool,  at the very top, hit the ellipses (...) next to the refresh icon where it says "Extensions".

Select the last option - "Install from vsix..."

Browse to your Downloads directory and select the extension you just installed.

## Configure VS Code

Hit ctrl+, (control comma), under extensions on the left, select Classroom Sync.

- Set "Auth Mode" to "none"
- For Instructor host use: 172.18.0.10
- For port: 3000
- For token: remove it - no token needed 

Should be good to go!

## Using it

In VSCode, in the ~/class directory you should be able to right-click on any file or directory and select "Sync from Instructor", or "Diff from Instructor". Sync will overwrite your version of something and replace it with mine. Diff will show the differences.

But what about files or directories you don't have?

Hit Ctrl+Shift+P in VS Code, and start typing "Classroom Sync" - you are looking for "Browse Instructor Content"

This should show you everything I have - just check what you need and hit enter.

