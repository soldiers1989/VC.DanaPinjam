#!/bin/bash
echo "open project dir."
cd /data/git/api.danapinjam.com/VC.DanaPinjam/

echo "get master version."
echo "1.git checkout master."

git checkout master

echo "2.git pull."
git pull

echo "3.git checkout api.danapinjam.com"
git checkout api.danapinjam.com

echo "4.git merge master"
git merge master

git pull

cd ./IF.AdminSystem/

echo "publish project."
/usr/share/dotnet/dotnet publish

echo "run test."
nohup /usr/share/dotnet/dotnet /data/git/api.danapinjam.com/VC.DanaPinjam/IF.AdminSystem/bin/Debug/netcoreapp2.1/publish/IF.AdminSystem.dll &

