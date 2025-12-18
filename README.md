# Саватеев Дмитрий, БПИ-248. Документация ДЗ№4 - «Гоzон»

## Краткое описание архитектуры системы

В рамках задания реализована микросервисная система интернет‑магазина, рассчитанная на рост трафика перед Новым годом. Система разделена на сервисы по зонам ответственности и общается через Kafka с гарантией at-least-once, а бизнес‑семантика списания денег гарантирует effectively exactly once

Проект состоит из следующих компонентов:

---

## 1. API Gateway (порт 8080)

**Назначение**: единая точка входа для внешних клиентов  
Gateway выполняет только маршрутизацию запросов и выдачу ошибок при недоступности сервисов, бизнес‑логики в нем нет

**Что делает**:
- принимает запросы от фронтенда и пользователя
- проксирует запросы в Orders Service и Payments Service

**Основные маршруты**:
- `/orders/*` -> Orders Service
- `/payments/*` -> Payments Service

---

## 2. Orders Service (порт 8081)

**Назначение**: микросервис для работы с заказами  
Сервис отвечает за создание заказа, просмотр списка заказов и просмотр статуса конкретного заказа

**Сущность заказа**:
- `OrderId` - уникальный id заказа
- `UserId` - пользователь, который сделал заказ (передаётся в запросе)
- `AmountKopeks` - сумма заказа в копейках
- `Description` - описание
- `Status` - `NEW` / `FINISHED` / `CANCELLED`

**Паттерн Transactional Outbox**:
- при создании заказа в одной транзакции сохраняются:
  - запись заказа
  - запись в outbox (сообщение на оплату)
- отдельный воркер читает outbox и публикует событие в Kafka

**Эндпоинты**:
- `POST /orders` - создать заказ и асинхронно запустить оплату
- `GET /orders` - получить список заказов
- `GET /orders/{orderId}`- получить статус конкретного заказа

---

## 3. Payments Service (порт 8082)

**Назначение**: микросервис для работы со счетами и оплатой  
Сервис отвечает за создание счета, пополнение и просмотр баланса. Также он принимает события об оплате заказа и списывает средства

**Паттерны Transactional Inbox + Outbox**:
- consumer читает событие оплаты из Kafka
- сохраняет его в inbox таблицу, гарантируя идемпотентность по ключу сообщения
- выполняет списание средств и формирует результат
- сохраняет событие результата в outbox
- отдельный воркер публикует результат в Kafka

**Семантика effectively exactly once при списании**:
- сообщение об оплате может прийти повторно (at-least-once)
- но списание за конкретный `order_id` выполняется максимум один раз
- это достигается поскольку:
  - inbox уникален по topic и message_key
  - ledger содержит idempotency ключ для списаний `ExternalRef = order_id` и уникален по `Kind, ExternalRef`

**Эндпоинты**:
- `POST /payments/accounts/create` - создать счет
- `POST /payments/accounts/topup` - пополнить счет
- `GET /payments/accounts/balance` - получить баланс  
---

## 4. Kafka (порт 29092)

**Назначение**: транспорт сообщений между микросервисами  
Используется модель at-least-once доставки сообщений. Offset коммитится только после успешной обработки

**Топики**:
- `gozon.payments.request.v1` - запрос на оплату заказа (Orders -> Payments)
- `gozon.payments.resolved.v1` - результат оплаты (Payments -> Orders)

---

## 5. PostgreSQL (порт 5432)

**Назначение**: хранение данных сервисов  
В проекте используются две базы:
- `gozon_orders`
- `gozon_payments`

### Основные таблицы Orders
- `Orders`
  - `Id` (PK)
  - `UserId`
  - `AmountKopeks`
  - `Description`
  - `Status`
  - `CreatedAtUtc`
  - `UpdatedAtUtc`
- `Outbox`
  - `Id` (PK)
  - `Topic`
  - `MessageKey`
  - `PayloadJson`
  - `PublishedAtUtc`
  - `Attempts`

### Основные таблицы Payments
- `Accounts`
  - `UserId` (PK)
  - `BalanceKopeks`
  - `CreatedAtUtc`
  - `UpdatedAtUtc`
- `Inbox`
  - `Id` (PK)
  - `Topic`
  - `MessageKey` (уникальный ключ для идемпотентности)
  - `PayloadJson`
  - `ReceivedAtUtc`
- `Ledger`
  - `Id` (PK)
  - `UserId`
  - `OrderId` (nullable)
  - `Kind` (`DEBIT_ORDER` / `TOP_UP`)
  - `ExternalRef` (строка, ключ идемпотентности)
  - `AmountKopeks`
  - `CreatedAtUtc`
- `Outbox`
  - `Id` (PK)
  - `Topic`
  - `MessageKey`
  - `PayloadJson`
  - `PublishedAtUtc`
  - `Attempts`

---

# Пользовательские сценарии

## Сценарий 1: создание счета
1. Клиент отправляет запрос в Payments через Gateway
2. Payments создает запись `Accounts` (если счета нет)
3. Возвращается `{ created: true }` или `{ created: false }`

---

## Сценарий 2: пополнение счета
1. Клиент вызывает `topup`
2. Payments увеличивает `BalanceKopeks`
3. В `Ledger` добавляется запись `TOP_UP` с уникальным `ExternalRef`

---

## Сценарий 3: создание заказа и автосписание
1. Клиент вызывает `POST /orders` через Gateway  
2. Orders в одной транзакции сохраняет:
   - заказ со статусом `NEW`
   - outbox событие `PaymentRequestedV1`
3. Воркер Orders публикует событие в Kafka topic `gozon.payments.request.v1`
4. Payments consumer читает событие и в одной транзакции:
   - сохраняет inbox
   - атомарно списывает деньги, если хватает средств
   - записывает ledger `DEBIT_ORDER` с `ExternalRef = order_id`
   - кладет outbox событие результата `PaymentResolvedV1`
5. Воркер Payments публикует результат в topic `gozon.payments.resolved.v1`
6. Orders consumer читает результат и обновляет статус заказа:
   - `FINISHED` если SUCCESS
   - `CANCELLED` если FAIL  
   обновление статуса идемпотентно

---

# Инструкция по использованию API через Swagger
!Во всех запросах должен быть заголовок:
- `X-User-Id: <guid>`

Для тестирования можно использовать любой guid
---

## 1. Payments Service Swagger
http://localhost:8082/swagger

### Создать счет
`POST /payments/accounts/create`

### Пополнить
`POST /payments/accounts/topup`  
Пример body:
```json
{
  "amountKopeks": 10000
}
```

### Получить баланс
`GET /payments/accounts/balance`  
Пример ответа:
```json
{
  "exists": true,
  "balanceKopeks": 7000
}
```

---

## 2. Orders Service Swagger
Открой: http://localhost:8081/swagger

### Создать заказ
`POST /orders`  
Пример body:
```json
{
  "amountKopeks": 3000,
  "description": "заказ к новому году"
}
```

### Список заказов
`GET /orders`

### Статус заказа
`GET /orders/{orderId}`

---

# Фронтенд
Фронтенд реализован отдельным сервисом `webui` и общается с бэкендом через REST API Gateway

Открыть:
- http://localhost:8090

Функционал:
- генерация `user_id`
- создание счета
- пополнение счета
- просмотр баланса
- создание заказа
- просмотр списка заказов и обновление статуса заказа  

---

# Инструкция по запуску

## 1.  Клонирование репозитория
```bash
git clone <repository-url>
cd GozonShop
```

## 2. Запуск системы
```bash
docker compose up --build
```

## 3. Доступ к API
- gateway: http://localhost:8080/
- orders: http://localhost:8081/
- payments: http://localhost:8082/
- ui: http://localhost:8090/

Swagger:
- orders swagger: http://localhost:8081/swagger
- payments swagger: http://localhost:8082/swagger

## 4.  Остановка системы
```bash
docker compose down -v
```
