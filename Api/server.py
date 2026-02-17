from fastapi import FastAPI, HTTPException
from fastapi.middleware.cors import CORSMiddleware
from fastapi.responses import JSONResponse
import json
import os
from typing import List, Dict, Any
from pydantic import BaseModel

app = FastAPI(
    title="Law API",
    description="API для доступа к юридическим данным",
    version="1.0.0"
)

# CORS - разрешаем запросы от WPF приложения
app.add_middleware(
    CORSMiddleware,
    allow_origins=["*"],  # В продакшене укажите конкретные домены
    allow_credentials=True,
    allow_methods=["*"],
    allow_headers=["*"],
)

DATA_DIR = "/app"  # Путь внутри контейнера

# Кэш для данных (чтобы не читать файл каждый раз)
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
    except json.JSONDecodeError:
        raise HTTPException(status_code=500, detail=f"Ошибка парсинга {filename}")

# Pydantic модели (для документации API)
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

# Эндпоинты
@app.get("/api/codeks", response_model=List[Codek])
async def get_codeks():
    """Получить список кодексов"""
    return load_json_cached('codeks.json')

@app.get("/api/laws", response_model=List[Law])
async def get_laws():
    """Получить список законов"""
    return load_json_cached('laws.json')

@app.get("/api/articles_full", response_model=List[ArticleFull])
async def get_articles_full():
    """Получить полный список статей"""
    return load_json_cached('articles_full.json')

@app.get("/api/text_articles", response_model=List[TextArticle])
async def get_text_articles():
    """Получить тексты статей"""
    return load_json_cached('text_new_articles.json')

# Health check
@app.get("/health")
async def health_check():
    """Проверка работоспособности API"""
    return {"status": "ok", "message": "API работает"}

# 🔄 Очистка кэша (полезно при обновлении данных)
@app.post("/api/cache/clear")
async def clear_cache():
    """Очистить кэш данных"""
    _data_cache.clear()
    return {"status": "ok", "message": "Кэш очищен"}

if __name__ == "__main__":
    import uvicorn
    # host='0.0.0.0' — обязательно для Docker!
    uvicorn.run(app, host="0.0.0.0", port=5000)