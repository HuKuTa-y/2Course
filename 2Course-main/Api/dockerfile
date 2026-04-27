FROM python:3.11-slim

# Устанавливаем curl для healthcheck
RUN apt-get update && apt-get install -y curl && rm -rf /var/lib/apt/lists/*

WORKDIR /app

# Копируем зависимости
COPY requirements.txt .

# Устанавливаем зависимости
RUN pip install --no-cache-dir -r requirements.txt

# Копируем приложение
COPY server.py .

# Копируем JSON файлы (резервная копия, если volumes не подключены)
COPY *.json ./

# Исправляем права на файлы
RUN chmod 644 /app/*.json /app/server.py

# Открываем порт
EXPOSE 5000

# Запуск приложения
CMD ["python", "server.py"]