#!/bin/bash

stdlib_modules=(errors io lists results strings maps)

mkdir -p "${HOME}/.aura"
mkdir -p "${HOME}/.aura/stdlib"
mkdir -p "${HOME}/.aura/prelude"

for mod in "${stdlib_modules[@]}" ; do
    mkdir -p "${HOME}/.aura/stdlib/${mod}"
    cp "./AuraLang.Stdlib/${mod}.go" "${HOME}/.aura/stdlib/${mod}"
done

cp "./AuraLang.Prelude/prelude.go" "${HOME}/.aura/prelude"

dotnet publish AuraLang.Cli -r osx-x64 --self-contained
mv ./AuraLang.Cli/bin/Debug/net7.0/osx-x64/publish/AuraLang.Cli ./AuraLang.Cli/bin/Debug/net7.0/osx-x64/publish/aura
mv ./AuraLang.Cli/bin/Debug/net7.0/osx-x64/publish/aura "${HOME}/.aura"
