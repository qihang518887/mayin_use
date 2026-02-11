#!/bin/bash

# NATMap
private_port=$4
public_port=$2
public_ip=$1

# Cloudflare
zone_id=""
email=""
global=""
srv_id=""
a_id=""

# Minecraft
external_domain=""
srv_domain=""

echo -e "Update UPNP."

upnpc -d ${private_port} tcp
upnpc -a 192.168.31.158 25565 ${private_port} tcp

echo -e "Update SRV Record."

# Update SRV Record.
curl --request PATCH \
  --url https://api.cloudflare.com/client/v4/zones/${zone_id}/dns_records/${srv_id} \
  --header "Content-Type: application/json" \
  --header "X-Auth-Email: ${email}"  \
  --header "X-Auth-Key: ${global}" \
  --data '{
	"type": "SRV",
	"name": "'"_minecraft._tcp.${external_domain}"'",
	"data": {
		"port": '"${public_port}"',
		"priority": 0,
                "weight": 0,
		"target": "'"${srv_domain}"'"
            }
}'

echo -e "Update A Record."

# Update A Record.
curl --request PATCH \
  --url https://api.cloudflare.com/client/v4/zones/${zone_id}/dns_records/${a_id} \
  --header "Content-Type: application/json" \
  --header "X-Auth-Email: ${email}"  \
  --header "X-Auth-Key: ${global}" \
  --data '{
	"type": "A",
	"name": "'"${srv_domain}"'",
	"content": "'"${public_ip}"'"
  }'
  
echo -e "\nDone."
