#!/bin/bash

iswin=0
if [ -d "/mnt/c" ]; then
	iswin=1
fi

if [ "$iswin" -eq 1 ]; then
	echo "Detected windows";
else
	echo "Deteced non-windows"
fi

dotnetcmd="dotnet"
if [ "$iswin" -eq 1 ]; then
	dotnetcmd="dotnet.exe"
fi

function build() {
	if [ "$iswin" -eq 1 ]; then
		"$dotnetcmd" build
	else
		pushd src
		dotnet build -p:Platform="x64"
		dotnet publish
		cp "bin/Debug/netcoreapp2.2/publish/x64/libcvextern.so" "bin/Debug/netcoreapp2.2/publish"
		popd
	fi
}

function run() {
	# build
	if [ "$iswin" -eq 1 ]; then
		cd "src/bin/Debug/netcoreapp2.2"
		"$dotnetcmd" exec ImageOpenCV.dll "$@"
	else
		cd "src/bin/Debug/netcoreapp2.2/publish"
		"$dotnetcmd" exec ImageOpenCV.dll "$@"
	fi
}

if [ -n "$1" ]; then
	$1 "${@:2}"
fi
