from fastapi import FastAPI, HTTPException, Query
from fastapi.middleware.cors import CORSMiddleware
import json
import os
from typing import List, Dict, Any, Optional
from pydantic import BaseModel
from urllib.parse import unquote


app = FastAPI(
    title="Law API",
    description="API для доступа к юридическим данным",
    version="2.0.0"
)

# CORS
app.add_middleware(
    CORSMiddleware,
    allow_origins=["*"],
    allow_credentials=True,
    allow_methods=["*"],
    allow_headers=["*"],
)

DATA_DIR = "/app"
_data_cache: Dict[str, Any] = {}


def load_json_cached(filename: str) -> Any:
    """Загружает JSON с кэшированием"""
    if filename in _data_cache:
        return _data_cache[filename]
    
    path = os.path.join(DATA_DIR, filename)
    
    if not os.path.exists(path):
        raise HTTPException(status_code=404, detail=f"Файл {filename} не найден")
    
    try:
        with open(path, 'r', encoding='utf-8') as f:
            data = json.load(f)
            _data_cache[filename] = data
            return data
    except PermissionError:
        raise HTTPException(status_code=500, detail=f"Нет прав доступа к {filename}")
    except json.JSONDecodeError as e:
        raise HTTPException(status_code=500, detail=f"Ошибка парсинга {filename}: {e}")


def extract_number_from_title(title: str) -> Optional[int]:
    """Извлекает число из названия статьи"""
    digits = ''.join(c for c in title if c.isdigit())
    return int(digits) if digits else None


class Codek(BaseModel):
    id: str
    Название: str
    Ссылка: str
    Номер: str

class Law(BaseModel):
    id: str
    Название: str
    Ссылка: str
    Номер: str

class ArticleFull(BaseModel):
    id: str
    Название: str
    Ссылка: str
    Номер_источника_статьи: str

class TextArticle(BaseModel):
    Название: str
    Контент: str


@app.get("/api/codeks", response_model=List[Codek])
async def get_codeks():
    """Список кодексов (только для кнопок)"""
    return load_json_cached('codeks.json')

@app.get("/api/laws", response_model=List[Law])
async def get_laws():
    """Список законов (только для кнопок)"""
    return load_json_cached('laws.json')


@app.get("/api/articles/by-source", response_model=List[ArticleFull])
async def get_articles_by_source(
    source_number: str = Query(..., description="Номер кодекса/закона, например: 51-ФЗ")
):
    """
    Получить статьи ТОЛЬКО указанного источника.
    
    Пример: GET /api/articles/by-source?source_number=51-ФЗ
    """
    all_articles = load_json_cached('articles_full.json')
    
    # Фильтрация НА СЕРВЕРЕ
    filtered = [
        a for a in all_articles 
        if a.get('Номер_источника_статьи') == source_number
    ]
    return filtered


@app.get("/api/article/text", response_model=TextArticle)
async def get_article_text(
    article_name: str = Query(..., description="Точное название статьи")
):
    """
    Получить текст ОДНОЙ статьи по названию.
    
    Пример: GET /api/article/text?article_name=Статья%20123
    """
    all_texts = load_json_cached('text_new_articles.json')
    
    # Поиск НА СЕРВЕРЕ
    matching = next(
        (t for t in all_texts if t.get('Название') == article_name),
        None
    )
    
    if matching:
        return matching
    
    raise HTTPException(
        status_code=404,
        detail=f"Текст статьи '{article_name}' не найден"
    )


@app.get("/api/search/by-number", response_model=List[ArticleFull])
async def search_by_number(
    number: int = Query(..., description="Номер статьи", ge=1)
):
    """
    Поиск статей по номеру (извлекается из названия).
    
    Пример: GET /api/search/by-number?number=123
    """
    all_articles = load_json_cached('articles_full.json')
    
    results = []
    for article in all_articles:
        title = article.get('Название', '')
        article_num = extract_number_from_title(title)
        if article_num == number:
            results.append(article)
    
    return results


@app.get("/api/search/by-text", response_model=List[ArticleFull])
async def search_by_text(
    query: str = Query(..., description="Поисковый запрос", min_length=1)
):
    """
    Поиск статей по тексту в названии.
    
    Пример: GET /api/search/by-text?query=договор%20аренда
    """
    all_articles = load_json_cached('articles_full.json')
    
    search_words = query.lower().split()
    results = []
    
    for article in all_articles:
        title = article.get('Название', '').lower()
        # Ищем статьи, содержащие ХОТЯ БЫ ОДНО слово из запроса
        if any(word in title for word in search_words):
            results.append(article)
    
    return results

@app.get("/health")
async def health_check():
    """Проверка работоспособности"""
    return {"status": "ok", "message": "API работает", "version": "2.0.0"}

@app.post("/api/cache/clear")
async def clear_cache():
    """Очистить кэш данных"""
    count = len(_data_cache)
    _data_cache.clear()
    return {"status": "ok", "message": f"Кэш очищен ({count} файлов)"}


if __name__ == "__main__":
    import uvicorn
    uvicorn.run(app, host="0.0.0.0", port=5000, log_level="info")