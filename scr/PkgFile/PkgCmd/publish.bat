del /s /q .\publish
md .\publish
copy .\Mir2TransMir3Infos.json .\publish\
copy .\urls.json .\publish\
copy ..\Release\PackageWrap.dll .\publish

dotnet publish .\PkgCmd.csproj -c debug -r win-x86 -o .\publish --sc /p:PublishSingleFile=true
pause