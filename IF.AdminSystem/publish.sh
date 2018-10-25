#!/bin/bash

cd /data/git/VC.DanaPinjam/
git checkout .
git pull
cd /data/git/VC.DanaPinjam/IF.AdminSystem/

echo "删除发布目录"
sudo rm -rf /data/www/api.smalldebit.club/release/

sudo mkdir /data/www/api.smalldebit.club/release -p

sudo dotnet publish -o /data/www/api.smalldebit.club/release -c release

current_date=`date -d "-1 day" "+%Y%m%d%H%M"`

echo "bak path is:$current_date"

echo "删除15天前的日志文件"
sudo find /data/www/api.smalldebit.club/logs/ -mtime +15 -name "*.log" -exec rm -rf {} \;

echo "清空nohup.out文件"
sudo echo /dev/null > /data/www/api.smalldebit.club/nohup.out
echo "开始备份"
sudo echo "做为版本记录:$current_date" > /data/www/api.smalldebit.club/version.out
sudo zip -r /data/www/$current_date.zip /data/www/api.smalldebit.club/*  > /dev/null 2>&1

echo "备份完成"
id=`sudo ps -aux |grep "dotnet"| awk '{if($12 == "'/data/www/api.smalldebit.club/IF.AdminSystem.dll'") {print $2}}'`
if [ "$id" == "" ]; then
        echo "dotnet IF.AdminSystem.dll is not running..."
else
        kill -9 $id
        echo "already kill IF.AdminSystem.dll."
fi
echo "将发布后的文件拷贝到着站点"
sudo \cp -r /data/www/api.smalldebit.club/release/* /data/www/api.smalldebit.club/

cd /data/www/api.smalldebit.club

sudo rm -rf /data/www/api.smalldebit.club/version.out
echo "还原配置文件"

sudo \cp appsettings.json.bak appsettings.json
sudo \cp dbinit.ini.bak dbinit.ini

echo "准备启动"
sudo nohup /usr/share/dotnet/dotnet /data/www/api.smalldebit.club/IF.AdminSystem.dll &

echo "成功"
ps -aux|grep dotnet
tailf nohup.out


#zip -r /data/www/test.zip /data/www/api.smalldebit.club/*

#版本还原
#unzip -o -d / /data/www/test.zip

#日期删除
#find /data/www/api.smalldebit.club/ -mtime +15 -name "*.log" -exec rm -rf {} \;