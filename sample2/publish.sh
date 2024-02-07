#!/usr/bin/env sh

rm -rf publish/
rm -rf bin/
rm -rf obj/
dotnet publish \
  -c Release \
  --sc \
  -o publish \
  -r win-x64 \
  -p:PublishSingleFile=true \
  -p:DebugType=embedded
ls -l publish/
cp publish/RmqProbe.exe $HOME/Downloads/.
