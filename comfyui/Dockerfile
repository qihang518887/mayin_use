FROM pytorch/pytorch:2.7.1-cuda12.8-cudnn9-runtime
WORKDIR /root
RUN apt update && \
apt install -y git && \
git clone https://github.com/comfyanonymous/ComfyUI.git && \
cd ComfyUI && \
pip install --no-cache-dir -r requirements.txt && \
pip install --no-cache-dir piexif && \
cd custom_nodes && \
git clone https://github.com/Comfy-Org/ComfyUI-Manager.git
CMD ["/opt/conda/bin/python", "/root/ComfyUI/main.py"]
