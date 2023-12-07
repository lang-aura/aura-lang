.PHONY: clean test publish format

clean:
	rm -rf ./AuraLang.Test/Integration/Examples/build/pkg/*.go

test:
	cd AuraLang.Test && dotnet test
	
install:
	./scripts/install.sh

format:
	dotnet format