.PHONY: clean test publish

clean:
	rm -rf ./AuraLang.Test/Integration/Examples/build/pkg/*.go

test:
	cd AuraLang.Test && dotnet test
	
publish:
	./scripts/publish.sh