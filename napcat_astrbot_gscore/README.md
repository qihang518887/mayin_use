# Napcat + AstrBot + GScore

Docker Compose 配置，用于在同一台主机上运行 Napcat、AstrBot 和 gsuid_core。容器使用 host 网络；配置与数据保存在当前目录的挂载目录中。

在本目录准备好 Napcat 配置后运行：

```sh
docker compose up -d
```

可通过 `NAPCAT_UID`、`NAPCAT_GID`、`DATA_PATH` 和 `PLUGIN_PATH` 设置用户及数据目录。更多配置见 [docker-compose.yml](docker-compose.yml)。
