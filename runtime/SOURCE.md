# runtime/ — source of the shared arm64 runtime package

These scripts are the source-of-truth copies that get bundled (alongside the patched
MelonLoader binaries) into the downloadable runtime tarball on the
[Releases page](https://github.com/datariot/btd6-mac-mods/releases/tag/arm64-runtime-v1).

They can't run standalone from here (the installer expects the `MelonLoader/` binaries
next to it, which live only in the release tarball — too large to commit to git). Edit
here, then rebuild the tarball from `macos-il2cpp-port/dist/` and re-upload the release asset.
