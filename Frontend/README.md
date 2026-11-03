# Fortress Booking — Frontend

Небольшое React-приложение (Vite, JavaScript) для добавления и просмотра коворкингов.
Обращается к бэкенду через Ocelot Gateway (см. папку `Gateway`).

## Запуск локально

```bash
npm install
npm run dev
```

Приложение поднимется на http://localhost:5173 и будет ходить в Gateway по адресу
из `VITE_API_BASE_URL` (по умолчанию http://localhost:5000, см. `.env`).

Перед запуском фронтенда должны быть подняты `Gateway`, `PartnerService` и `UserService`.

## Сборка

```bash
npm run build
```

Собранные файлы появятся в `dist/`.

## Docker

Образ собирается через `Dockerfile` в этой папке (multi-stage: сборка на Node,
раздача статики через nginx). Адрес API передаётся как build-arg `VITE_API_BASE_URL`,
так как Vite "запекает" переменные окружения в бандл на этапе сборки.
