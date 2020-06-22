# SharpQuake

### Description 

SharpQuake is a **[GLQuake](https://github.com/dpteam/GLQuake3D)** reimplementation in C# using the **[OpenTK](https://github.com/opentk/opentk)** library.

### Dependencies
* OpenTK 3.2.0
* NVorbis 0.10.1
* **[OpenAL](https://www.openal.org/downloads/)** (Windows) / libopenal on Linux
* **[SDL2](https://www.libsdl.org/download-2.0.php)** (Windows and macOS) / libsdl2-2.0 on Linux (Runtime binaries)
  
### Building

**Project is built against and tested for Jetbrains Rider on .NET 4.8.0 (C# 8.0)**

1) **Add the required packages if needed by searching for them in the NuGet package manager or installing them manually with:**
    - `Install-Package OpenTK -Version 3.2.0`
    - `Install-Package NVorbis -Version 0.10.1`
    - `Install-Package NVorbis.OpenTKSupport -Version 1.4.0`
    - Other Requirements will be added automatically by Rider.

2) **Add dependencies for your target architecture in your output directory (defaults to `<project root>/Quake`).**
    - See links under dependencies header above
3) **Add ld1, hipnotic, and rogue (minimum ld1) data directories to `<project root>/Quake`.**
    - You only need the `<mod>\pak0.pak`, etc. and `<mod>\config.cfg` files of each directory if copying from a steam install.

4) **Build solution.**

### Running

In case you don't own a copy of Quake you can purchase it on **[GOG.com](https://www.gog.com/game/quake_the_offering)**, **[Steam](https://store.steampowered.com/app/2310/QUAKE/)** or use the **[shareware version](https://community.pcgamingwiki.com/files/file/411-quake-shareware-pak/)**.

**As Linux is case-sensitive make sure your folder and file names look like that `Id1` and `PAK0.PAK` and `PAK1.PAK` (in case you own the full version).**

**To play with soundtrack add it to `Id1/music` or the mission pack folder you want to play in OGG Vorbis format like this `track02.ogg`. You can get it here** **[Steam Guide](https://steamcommunity.com/sharedfiles/filedetails/?id=119489135)**

**macOS / OSX is not official supported as OpenGL on Mac is poorly supported and has been replaced in favour of their own graphics API [Metal](https://en.wikipedia.org/wiki/Metal_(API)).**

The `-basedir <directory>` switch can be used to change the default data directory. e.g. `SharpQuake.exe -basedir C:\Quake\`

`-window` switch is pre-defined in project settings, but if you change the default directory of where you want your data, you may need to add `-basedir <directory>` to `project properties > Debug > Command line arguments` 

The original expansion packs can be run using `-rogue` or `-hipnotic` switches if the data is present for it.

Original Quake switches apply and can be used.

Enjoy! 🙂

![](https://user-images.githubusercontent.com/1466920/56814073-a068e200-683e-11e9-8e90-b75ca617d9ce.png)

### Credits
* Made by **[yurykiselev](https://sourceforge.net/u/yurykiselev/profile/)** and **Uze** and brought to Github by **[Memorix101](https://github.com/Memorix101)**

* Updated to .NET 4.8.0 (C# 8.0), NVorbis 0.10.1, and OpenTK 3.2.0 by **[Daniel Cornelius (Kerfuffles/NukeAndBeans)](https://github.com/Kerfuffles)**

* Engine additions and fixes by **[multiguy18](https://github.com/multiguy18)** and **[Memorix101](https://github.com/Memorix101)**

* Original source code on **[SourceForge.net](https://sourceforge.net/projects/sharpquake/)**
