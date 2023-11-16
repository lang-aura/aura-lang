.PHONY: test publish

test:
	cd AuraLang.Test && dotnet test
	
publish:
	./scripts/publish.sh