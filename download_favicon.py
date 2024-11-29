import os
import requests
from bs4 import BeautifulSoup
from urllib.parse import urlparse, urljoin

def get_favicon_url(site_url):
    # Отправляем запрос на сайт
    response = requests.get(site_url)
    if response.status_code != 200:
        print(f"Ошибка при подключении к сайту: {site_url}")
        return None

    # Парсим HTML-страницу
    soup = BeautifulSoup(response.text, 'html.parser')
    
    # Ищем тег <link> для favicon
    link_tag = soup.find('link', rel=lambda rel: rel and 'icon' in rel.lower())
    if link_tag:
        favicon_url = link_tag.get('href')
        return urljoin(site_url, favicon_url)
    return None

def download_favicon(favicon_url):
    # Скачиваем favicon
    if favicon_url:
        response = requests.get(favicon_url)
        if response.status_code == 200:
            # Сохраняем favicon
            favicon_name = os.path.basename(urlparse(favicon_url).path)
            with open(favicon_name, 'wb') as f:
                f.write(response.content)
            print(f"Фавикон сохранен как {favicon_name}")
        else:
            print(f"Не удалось скачать favicon: {favicon_url}")
    else:
        print("Favicon не найден.")

if __name__ == "__main__":
    site_url = "https://www.wikipedia.org"  
    favicon_url = get_favicon_url(site_url)
    download_favicon(favicon_url)
