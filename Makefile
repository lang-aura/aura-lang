.PHONY: clean test publish format build

clean:
	python3 ./scripts/clean.py

test: build
	cd AuraLang.Test && dotnet test
	
install: build
	./scripts/install.sh

format:
	dotnet format

build:
	dotnet build