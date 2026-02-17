from flask import Flask, jsonify
from flask_cors import CORS
import json
import os

app = Flask(__name__)
CORS(app)  # Разрешает запросы с других доменов (например, из WPF)

DATA_DIR = "."

def load_json(filename):
    path = os.path.join(DATA_DIR, filename)
    with open(path, 'r', encoding='utf-8') as f:
        return json.load(f)

@app.route('/api/codeks')
def get_codeks():
    return jsonify(load_json('codeks.json'))

@app.route('/api/laws')
def get_laws():
    return jsonify(load_json('laws.json'))

@app.route('/api/articles_full')
def get_articles_full():
    return jsonify(load_json('articles_full.json'))

@app.route('/api/text_articles')
def get_text_articles():
    return jsonify(load_json('text_new_articles.json'))

if __name__ == '__main__':
    # host='0.0.0.0' — ОБЯЗАТЕЛЬНО для доступа извне контейнера!
    app.run(host='0.0.0.0', port=5000, debug=False)