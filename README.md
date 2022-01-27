# Uma Musume: Pretty Derby Toolbox
.NET CLI implementation of [Rockisch's umamusu-utils](https://github.com/rockisch/umamusu-utils) project. Thank you Rockisch! I couldn't have done this without your script!

I have recreated the [`data_download.py`](https://github.com/rockisch/umamusu-utils/blob/master/scripts/data_download.py) script and added a config file called, [`appsettings.json`](https://github.com/SimpleSandman/UmaMusumeToolbox/blob/master/UmaMusumeToolbox.DataDownload/appsettings.json). It's already in the repo if you want to check it out!

I'll work on a VS code configuration and make this Linux friendly once I finish what I need to do.

# Config File Explanation
- IsDebugMode
  - If set to `true`, this will output additional info in your console, specifically when it is downloading a single file and has finished downloading that file. Creates a lot of text on the screen, but if you want to keep a closer eye on the progress, this is here for you.
  - If set to `false`, then all you see are error messages and "complete X of Y files". The progress message will only show up when it's taking on another batch of files.
- MasterDbFilepath
  - This file is the core of where all of the data is stored for this game. Typically on a Windows machine using DMM, the `master.mdb` file is stored in the path below. The reason for the double backslash `\\` is to "escape" the backslash character. Please don't forget this if you're using this on a Windows machine!
  - Typical path: `C:\\Users\\<your_username>\\AppData\\LocalLow\\Cygames\\umamusume\\master\\master.mdb`
- MetaDbFilepath
  - This file is the meta file to grab the assets of the game. Please read above why the `\\` exist in this example.
  - Typical path: `C:\\Users\\<your_username>\\AppData\\LocalLow\\Cygames\\umamusume\\meta`
- SkipExistingFiles
  - If set to `true`, this will only attempt to download missing files in the saved location you pointed this program to.
  - If set to `false`, this will download all of the files, regardless if they exist already.
- TimeoutFromMinutes
  - I personally have mine set to 5 minutes on a 5 MB/s download because the default 100 seconds didn't work for me for some of the bigger assets.
- UserSavedLocation
  - This is where your downloaded files will be stored. It will create the directory path if it doesn't exist. Make sure you have permission to the file path you've set this to. I picked the `%USERPROFILE%` location because its the current user's directory.
  - Example path: `C:\\Users\\<your_username>\\uma_storage`