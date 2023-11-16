dotnet publish AuraLang.Cli -r osx-x64 --self-contained
mv ./AuraLang.Cli/bin/Debug/net7.0/osx-x64/publish/AuraLang.Cli ./AuraLang.Cli/bin/Debug/net7.0/osx-x64/publish/aura
sudo mv ./AuraLang.Cli/bin/Debug/net7.0/osx-x64/publish/aura /usr/local/bin
