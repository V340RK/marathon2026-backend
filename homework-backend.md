# Домашнє завдання

## 1й рівень складності

Реалізувати API для отримання списку сесій учасників групового pre-order для Owner

**GET /guest/groups/{groupReservationId}/sessions**

**Опис:**  
Owner має бачити, які учасники приєдналися до групового замовлення, які страви вони додали, загальну суму по кожному учаснику та загальну суму по всіх учасниках.

**Response:**

```json
{
  "participants": [
    {
      "id": "session1",
      "type": "Guest",
      "name": "Andrii",
      "items": [
        {
          "dish": {
            "id": "dish1",
            "name": "Caesar Salad",
            "price": 180
          },
          "quantity": 2
        }
      ],
      "total": 360,
      "isSubmitted": true
    },
    {
      "id": "session2",
      "type": "Guest",
      "name": "Olena",
      "items": [
        {
          "dish": {
            "id": "dish2",
            "name": "Tom Yum",
            "price": 220
          },
          "quantity": 1
        }
      ],
      "total": 220,
      "isSubmitted": false
    }
  ],
  "total": 580
}
```

---

## 2й рівень складності

Реалізувати функціонал для Owner, який дозволяє:
- отримати `guestSessionId` власної owner session для конкретного reservation;
- підтвердити консолідований груповий pre-order.

### 1. Отримати owner session id

**GET /reservations/{reservationId}/guest-session-id**

**Response:**

```json
{
  "guestSessionId": "owner-session-123"
}
```

### 2. Підтвердити консолідований pre-order

**POST /group-reservations/{groupReservationId}/submit**

**Request:**

```json
{
  "reservationId": "reservation-123"
}
```

**Опис:**  
Після перегляду замовлень усіх учасників Owner має підтвердити консолідований pre-order, який буде переданий ресторану для подальшої обробки.

Під час submit потрібно:
- взяти лише `submitted` guest sessions;
- зібрати всі страви в один список;
- об’єднати однакові страви по `dishId`;
- додати їх в order;
- змінити статус order на `SubmittedByCustomer`;
- змінити статус group reservation на `Submitted`.

**Response:**

```json
[
  {
    "dishId": "dish1",
    "quantity": 3
  },
  {
    "dishId": "dish2",
    "quantity": 2
  }
]
```

**Умови:**
- тільки Owner може викликати обидва endpoint-и;
- якщо reservation або group reservation не знайдено — повернути 404;
- якщо немає жодної `submitted` session — повернути 400;
- якщо в submitted sessions немає жодної страви — повернути 400;
- якщо order не знайдено — повернути 400.

---

## 3й рівень складності

Розширити функціонал керування груповим pre-order для Owner:
- реалізувати видалення guest session із групи;
- після видалення повертати оновлений список учасників і нову загальну суму.

**DELETE /guest/groups/{groupReservationId}/sessions/{guestSessionId}**

**Опис:**  
Owner може видалити сесію конкретного учасника з групового замовлення.

**Response:**

```json
{
  "participants": [
    {
      "id": "session2",
      "type": "Guest",
      "name": "Olena",
      "items": [
        {
          "dish": {
            "id": "dish2",
            "name": "Tom Yum",
            "price": 220
          },
          "quantity": 1
        }
      ],
      "total": 220,
      "isSubmitted": false
    }
  ],
  "total": 220
}
```

**Умови:**
- тільки Owner може видаляти guest session;
- якщо guest session не належить цій group reservation — повернути 400;
- якщо guest session не знайдена — повернути 404;
- після видалення потрібно повернути оновлений snapshot групи.
