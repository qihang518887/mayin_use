zone_id="添加区域id"

email="你的cf邮箱"

global="全局api"

srv_id="mc访问的域名的recordid"

a_id="srv目标域名的recordid"

external_domain="srv目标域名"

srv_domain="mc访问的域名"

获取recordid

curl --request GET   --url https://api.cloudflare.com/client/v4/zones/{zone_id}/dns_records   --header "X-Auth-Key: {global_api}"   --header "X-Auth-Email: {email}"   --header "Content-Type: application/json"

在返回值中查找对应name记录的id

变量填好后，执行 ./natmap-linux-x86_64 -s stun.cloudflare.com -h qq.com -b 0 -e ./update.sh

特别鸣谢
natmap
