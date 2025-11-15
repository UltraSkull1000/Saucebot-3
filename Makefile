.PHONY: clean

build ./Saucebot/Saucebot.sln:
	dotnet build 

run ./Saucebot/bin/Debug/net8.0/Saucebot.dll:
	dotnet ./Saucebot/bin/Debug/net8.0/Saucebot.dll

clean:
	rm -rf ./Saucebot/bin
	rm -rf ./Saucebot/obj

