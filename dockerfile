FROM python:3.9-slim
RUN pip install --no-cache-dir requests beautifulsoup4
COPY download_favicon.py /APP/download_favicon.py
WORKDIR /APP
CMD ["python", "download_favicon.py"]
