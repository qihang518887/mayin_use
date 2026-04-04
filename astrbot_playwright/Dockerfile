FROM soulter/astrbot:latest
RUN cd AstrBot/data/plugins && git clone https://github.com/luminacry/astrbot_plugin_zhenxunribao && cd astrbot_plugin_zhenxunribao && pip install -r requirements.txt && playwright install --with-deps chromium
