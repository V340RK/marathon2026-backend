# API Routes

## Auth

### `POST` /register
```json
{
    "firstName": "John",
    "lastName": "Doe",
    "email": "john_doe@mail.com",
    "password": "1234SecurePassword!",
    "confirmPassword": "1234SecurePassword!"
}
```

Response: 
`200 OK`
```json
{
    "message":"User registered and confirmed successfully"
}
```

### `POST` /signin
```json
{
    "email": "john_doe@mail.com",
    "password": "1234SecurePassword!",
}
```

Response: `200 OK`
```json
{
    "message": "Sign in successful",
    "idToken": "token",
    "accessToken": "access token",
}
```

## Feedback

### `GET` /feedback
Query parameters:
- `locationId` (string, required)
- `type` (Service | CuisineExperience)
- `sort` (TopRatedFirst | LowRatedFirst | NewestFirst | OldestFirst)
- `page` (integer, required)

Response: `200 OK`
```json
{
  "items": [
    {
      "reservationId": "string",
      "locationId": "string",
      "userId": "string|null",
      "comment": "string|null",
      "rating": 5,
      "createdAt": "2026-01-01T12:00:00Z",
      "type": "string",
      "username": "string|null"
    }
  ],
  "totalPages": 1,
  "currentPage": 1
}
```

### `POST` /feedback
Request body:
```json
{
  "reservationId": "string",
  "userId": "string|null",
  "comment": "string|null",
  "rating": 5,
  "type": "Service | CuisineExperience"
}
```

Response: `200 OK`
```json
{
  "reservationId": "string",
  "locationId": "string",
  "userId": "string|null",
  "comment": "string|null",
  "rating": 5,
  "createdAt": "2026-01-01T12:00:00Z",
  "type": "string",
  "username": "string|null"
}
```

### `PUT` /feedback
Request body:
```json
{
  "reservationId": "string",
  "userId": "string|null",
  "comment": "string|null",
  "rating": 5,
  "type": "string"
}
```

Response: `200 OK`
```json
{
  "message": "Feedback updated successfully."
}
```

## Locations

### `POST` /locations
Request body:
```json
{
  "address": "string",
  "description": "string",
  "totalCapacity": 50,
  "averageOccupancy": "string",
  "imageBase64": "string|null",
  "rating": 4.5
}
```

Response: `201 Created`
```json
{
  "id": "string",
  "address": "string",
  "description": "string",
  "totalCapacity": 50,
  "averageOccupancy": "string",
  "imageUrl": "string|null",
  "rating": 4.5
}
```

### `GET` /locations
Response: `200 OK`
```json
[
  {
    "id": "string",
    "address": "string",
    "description": "string",
    "totalCapacity": 50,
    "averageOccupancy": "string",
    "imageUrl": "string|null",
    "rating": 4.5
  }
]
```

### `GET` /locations/{id}/dishes
Response: `200 OK`
```json
[
  {
    "id": "string",
    "name": "string",
    "imageUrl": "string",
    "price": 12.34,
    "state": "string",
    "weight": "string",
    "dishType": "string",
    "description": "string",
    "calories": "string",
    "proteins": "string",
    "fats": "string",
    "carbohydrates": "string",
    "vitamins": "string"
  }
]
```

### `DELETE` /locations/{locationId}/dishes/{dishId}
Response: `200 OK`
```json
{
  "message": "Dish removed from location successfully."
}
```


### `GET` /locations/{id}
Response: `200 OK`
```json
{
  "id": "string",
  "address": "string",
  "description": "string",
  "totalCapacity": 50,
  "averageOccupancy": "string",
  "imageUrl": "string|null",
  "rating": 4.5
}
```

### `PUT` /locations/{id}
Request body:
```json
{
  "address": "string",
  "description": "string",
  "totalCapacity": 50,
  "averageOccupancy": "string",
  "imageBase64": "string|null",
  "rating": 4.5
}
```
Response: 200 OK
```json
{
  "id": "string",
  "address": "string",
  "description": "string",
  "totalCapacity": 50,
  "averageOccupancy": "string",
  "imageUrl": "string|null",
  "rating": 4.5
}
```

### `DELETE` /locations/{id}
Response: 200 OK
```json
{
  "message": "Location deleted successfully"
}
```

## Waiters

### `POST` /waiters
Request body:
```json
{
  "email": "waiter@example.com",
  "locationId": "string"
}
```

Response: `200 OK`
```json
{
  "message": "Email added successfully"
}
```

### `GET` /waiters/{userId}/location
Response: `200 OK`
```json
{
  "locationId": "string"
}
```

### `DELETE` /waiters
Request body:
```json
{
  "email": "waiter@example.com",
  "locationId": "string"
}
```

Response: `200 OK`
```json
{
  "message": "Email deleted successfully"
}
```

## Dishes

### `POST` /dishes
Request body:
```json
{
  "name": "string",
  "description": "string",
  "dishType": "string",
  "price": 12.34,
  "weight": "string",
  "calories": "string",
  "proteins": "string",
  "fats": "string",
  "carbohydrates": "string",
  "vitamins": "string",
  "imageBase64": "string|null",
  "state": "string",
  "locationIds": ["string"]
}
```

Response: `201 Created`
```json
{
  "id": "string",
  "name": "string",
  "description": "string",
  "dishType": "string",
  "price": 12.34,
  "weight": "string",
  "calories": "string",
  "proteins": "string",
  "fats": "string",
  "carbohydrates": "string",
  "vitamins": "string",
  "imageUrl": "string",
  "state": "string"
}
```

### `GET` /dishes
Optional query parameters:
- `dishType` (Appetizer | MainCourse | Dessert)
- `sortBy` (PriceAscending, PriceDescending, PopularityAscending, PopularityDescending)
- `name`

Response: `200 OK`
```json
[
  {
    "id": "string",
    "name": "string",
    "imageUrl": "string",
    "price": 12.34,
    "state": "string",
    "weight": "string",
    "dishType": "string",
    "description": "string",
    "calories": "string",
    "proteins": "string",
    "fats": "string",
    "carbohydrates": "string",
    "vitamins": "string"
  }
]
```

### `PUT` /dishes/{id}
Request body:
```json
{
  "name": "string",
  "description": "string",
  "dishType": "string",
  "price": 12.34,
  "weight": "string",
  "calories": "string",
  "proteins": "string",
  "fats": "string",
  "carbohydrates": "string",
  "vitamins": "string",
  "imageBase64": "string|null",
  "state": "string"
}
```
Response: 200 OK
```json
{
  "id": "string",
  "name": "string",
  "description": "string",
  "dishType": "string",
  "price": 12.34,
  "weight": "string",
  "calories": "string",
  "proteins": "string",
  "fats": "string",
  "carbohydrates": "string",
  "vitamins": "string",
  "imageUrl": "string",
  "state": "string"
}
```

### `DELETE` /dishes/{id}
Query parameters:
  force (boolean, optional)
Response: 200 OK
```json
{
  "message": "Dish '{id}' deleted successfully."
}
```


### `GET` /dishes/popular
Response: `200 OK`
```json
[
  {
    "id": "string",
    "name": "string",
    "imageUrl": "string",
    "price": 12.34,
    "state": "string",
    "weight": "string",
    "dishType": "string",
    "description": "string",
    "calories": "string",
    "proteins": "string",
    "fats": "string",
    "carbohydrates": "string",
    "vitamins": "string"
  }
]
```

### `GET` /dishes/{id}
Response: `200 OK`
```json
{
  "id": "string",
  "name": "string",
  "description": "string",
  "dishType": "string",
  "price": 12.34,
  "weight": "string",
  "calories": "string",
  "proteins": "string",
  "fats": "string",
  "carbohydrates": "string",
  "vitamins": "string",
  "imageUrl": "string",
  "state": "string"
}
```

## Reservations

### `POST` /reservations
Request body:
```json
{
  "tableId": "string",
  "customerEmail": "string|null",
  "startTime": "2026-08-07T19:00:00Z",
  "endTime": "2026-08-07T20:30:00Z",
  "guestCount": 4,
  "customerId": "string|null"
}
```

Response: `201 Created`
```json
{
  "id": "string",
  "tableId": "string",
  "startTime": "2026-08-07T19:00:00Z",
  "endTime": "2026-08-07T20:30:00Z",
  "guestCount": 4,
  "status": "Reserved",
  "customerId": "string|null",
  "waiterId": "string",
  "locationAddress": "string",
  "restaurantName": "Green & Tasty",
  "hasSubmittedPreOrder": false
}
```

### `GET` /reservations
Response: `200 OK`
```json
[
  {
    "id": "string",
    "tableId": "string",
    "startTime": "2026-08-07T19:00:00Z",
    "endTime": "2026-08-07T20:30:00Z",
    "guestCount": 4,
    "status": "Reserved",
    "customerId": "string|null",
    "waiterId": "string",
    "locationAddress": "string",
    "restaurantName": "Green & Tasty",
    "hasSubmittedPreOrder": false
  }
]
```

### `GET` /reservations/{reservationId}
Response: `200 OK`
```json
{
  "id": "string",
  "tableId": "string",
  "startTime": "2026-08-07T19:00:00Z",
  "endTime": "2026-08-07T20:30:00Z",
  "guestCount": 4,
  "status": "Reserved",
  "customerId": "string|null",
  "waiterId": "string",
  "locationAddress": "string",
  "restaurantName": "Green & Tasty",
  "hasSubmittedPreOrder": false
}
```

### `PATCH` /reservations/{reservationId}/status
Request body:
```json
{
  "newStatus": "Completed"
}
```

Response: `200 OK`
```json
{
  "message": "Reservation status updated successfully."
}
```

### `GET` /reservations/{reservationId}/invite-token
Response: `200 OK`
```json
{
  "token": "string"
}
```

### `GET` /reservations/{reservationId}/feedback
Response: `200 OK`
```json
{
  "service": {
    "reservationId": "string",
    "locationId": "string",
    "userId": "string|null",
    "comment": "string|null",
    "rating": 5,
    "createdAt": "2026-08-07T21:00:00Z",
    "type": "string",
    "username": "string|null"
  },
  "cuisineExperience": {
    "reservationId": "string",
    "locationId": "string",
    "userId": "string|null",
    "comment": "string|null",
    "rating": 5,
    "createdAt": "2026-08-07T21:00:00Z",
    "type": "string",
    "username": "string|null"
  }
}
```

### `GET` /reservations/{reservationId}/guest-session-id
Response: `200 OK`
```json
{
  "guestSessionId": "string"
}
```


### `DELETE` /reservations
Request body:
```json
{
  "id": "string"
}
```

Response: `200 OK`
```json
{
  "message": "Reservation cancelled successfully"
}
```

### `PUT` /reservations
Request body:
```json
{
  "reservationId": "string",
  "startTime": "2026-08-07T19:15:00Z",
  "endTime": "2026-08-07T20:45:00Z",
  "guestCount": 5
}
```

Response: `200 OK`
```json
{
  "id": "string",
  "tableId": "string",
  "startTime": "2026-08-07T19:15:00Z",
  "endTime": "2026-08-07T20:45:00Z",
  "guestCount": 5,
  "status": "Reserved",
  "customerId": "string|null",
  "waiterId": "string",
  "locationAddress": "string",
  "restaurantName": "Green & Tasty",
  "hasSubmittedPreOrder": false
}
```

### `PATCH` /reservations/{reservationId}/order/confirmations/{dishId}
Response: `200 OK`
```json
{
  "message": "Pre order confirmed successfully"
}
```

### `PATCH` /reservations/{reservationId}/order/cancellations/{dishId}
Response: `200 OK`
```json
"Pre order dish canceled successfully"
```

### `PATCH` /reservations/{reservationId}/table
Request body:
```json
{
  "tableId": "string"
}
```

Response: `200 OK`
```json
"Your reservation table was updated successfully."
```

## Tables

### `POST` /tables
Request body:
```json
{
  "locationId": "string",
  "capacity": 4,
  "tableNumber": 10
}
```

### `PUT` /tables/{id}
Request body:
```json
{
  "capacity": 4,
  "tableNumber": 10
}
```
Response: 200 OK
```json
{
  "id": "string",
  "locationId": "string",
  "capacity": 4,
  "tableNumber": 10
}
```

### `DELETE` /tables/{id}
Response: 200 OK
```json
{
  "message": "Table deleted successfully"
}
```

Response: `201 Created`
```json
{
  "id": "string",
  "locationId": "string",
  "capacity": 4,
  "tableNumber": 10
}
```

### `GET` /available-tables
Query parameters:
- `locationId` (string, optional)
- `capacity` (integer, optional)
- `startTime` (ISO 8601 string, optional)

Response: `200 OK`
```json
[
  {
    "id": "string",
    "locationId": "string",
    "capacity": 4,
    "tableNumber": 10
  }
]
```

### `GET` /locations/{locationId}/tables/all
Response: `200 OK`
```json
[
  {
    "id": "string",
    "locationId": "string",
    "capacity": 4,
    "tableNumber": 10
  }
]
```

### `GET` /tables/{id}/available-time-slots
Query parameters:
- `startTime` (ISO 8601 string, required)

Response: `200 OK`
```json
[
  {
    "start": "2026-08-07T19:00:00Z",
    "end": "2026-08-07T20:30:00Z"
  }
]
```

## User Profile

### `GET` /me
Response: `200 OK`
```json
{
  "id": "string",
  "firstName": "string",
  "lastName": "string",
  "email": "string",
  "role": "string"
}
```

### `PUT` /me
Request body:
```json
{
  "firstName": "string",
  "lastName": "string"
}
```

Response: `200 OK`
```json
"User information updated successfully."
```

### `PUT` /me/change-password
Request body:
```json
{
  "oldPassword": "string",
  "newPassword": "string",
  "accessToken": "string"
}
```

Response: `200 OK`
```json
{
  "message": "Password changed successfully."
}
```

### `GET` /users
Query parameters:
- `fullName` (string, required)

Response: `200 OK`
```json
[
  {
    "id": "string",
    "firstName": "string",
    "lastName": "string",
    "email": "string",
    "role": "string"
  }
]
```

### `GET` /users/{id}/profile-image
Response: `200 OK`
```json
{
  "presignedUrl": "https://..."
}
```

If the user has not uploaded an image yet, the route returns:
```json
"Profile image is not uploaded so far."
```

### `PUT` /me/profile-image
Request body:
```json
{
  "base64Image": "string"
}
```

Response: `200 OK`
```json
"Photo was uploaded successfully!"
```

### `GET` /users/{id}/name
Response: `200 OK`
```json
{
  "firstName": "string",
  "lastName": "string"
}
```

## Orders

### `GET` /orders
Response: `200 OK`
```json
[
  {
    "orderId": "string",
    "reservationId": "string",
    "reservationAddress": "string",
    "reservationTime": "string",
    "orderItems": [
      {
        "dish": {
          "id": "string",
          "name": "string",
          "description": "string",
          "dishType": "string",
          "price": 12.34,
          "weight": "string",
          "calories": "string",
          "proteins": "string",
          "fats": "string",
          "carbohydrates": "string",
          "vitamins": "string",
          "imageUrl": "string",
          "state": "string"
        },
        "quantity": 2
      }
    ],
    "status": "Created"
  }
]
```

### `GET` /orders/{reservationId}
Response: `200 OK`
```json
{
  "orderId": "string",
  "reservationId": "string",
  "reservationAddress": "string",
  "reservationTime": "string",
  "orderItems": [
    {
      "dish": {
        "id": "string",
        "name": "string",
        "description": "string",
        "dishType": "string",
        "price": 12.34,
        "weight": "string",
        "calories": "string",
        "proteins": "string",
        "fats": "string",
        "carbohydrates": "string",
        "vitamins": "string",
        "imageUrl": "string",
        "state": "string"
      },
      "quantity": 2
    }
  ],
  "status": "Created"
}
```

### `POST` /orders
Request body:
```json
{
  "reservationId": "string",
  "orderItems": [
    {
      "dishId": "string",
      "quantity": 2
    }
  ]
}
```

Response: `201 Created`
```json
{
  "orderId": "string",
  "reservationId": "string",
  "reservationAddress": "string",
  "reservationTime": "string",
  "orderItems": [
    {
      "dish": {
        "id": "string",
        "name": "string",
        "description": "string",
        "dishType": "string",
        "price": 12.34,
        "weight": "string",
        "calories": "string",
        "proteins": "string",
        "fats": "string",
        "carbohydrates": "string",
        "vitamins": "string",
        "imageUrl": "string",
        "state": "string"
      },
      "quantity": 2
    }
  ],
  "status": "Created"
}
```

### `PUT` /orders/{reservationId}
Request body:
```json
{
  "orderItems": [
    {
      "dishId": "string",
      "quantity": 3
    }
  ]
}
```

Response: `200 OK`
```json
"Pre-order updated successfully."
```

### `DELETE` /orders/{reservationId}
Response: `200 OK`
```json
"Pre-order canceled successfully."
```

### `POST` /orders/{reservationId}/submit
Response: `200 OK`
```json
"Pre-order submitted successfully."
```

### `POST` /orders/{reservationId}/unsubmit
Response: `200 OK`
```json
"Pre-order unsubmitted successfully."
```

## Reports

### `GET` /reports/download
Query parameters:
- `format` (string, optional: Excel, Pdf, Csv)
- `type` (string, optional: Service, Sales)
- `startDate` (YYYY-MM-DD, optional)
- `endDate` (YYYY-MM-DD, optional)
- `locationId` (string, required)

Response: `200 OK` with file download content

### `GET` /reports/generate
Query parameters:
- `type` (string, optional: Service, Sales)
- `startDate` (YYYY-MM-DD, required)
- `endDate` (YYYY-MM-DD, required)
- `locationId` (string, required)

Response: `200 OK`
```json
[
  {
    "reportType": "Service",
    "startDate": "2026-08-01",
    "endDate": "2026-08-07",
    "userId": "string",
    "locationId": "string",
    "locationName": "string",
    "waiterId": "string",
    "waiterName": "string",
    "waiterEmail": "string",
    "workingHours": 72.5,
    "totalOrders": 28,
    "avgServiceFeedback": 4.7,
    "minServiceFeedback": 3.8,
    "deltaAvgServiceFeedback": 1.2,
    "deltaTotalOrders": 5.0
  }
]
```

For `type=Sales`, the response is an array of sales report history objects:
```json
[
  {
    "reportType": "Sales",
    "startDate": "2026-08-01",
    "endDate": "2026-08-07",
    "userId": "string",
    "locationId": "string",
    "locationName": "string",
    "totalOrders": 147,
    "avgCuisineFeedback": 4.3,
    "minCuisineFeedback": 2.5,
    "revenue": 32145.60,
    "deltaAvgCuisineFeedback": 2.1,
    "deltaRevenue": 8.4
  }
]
```

### `GET` /reports
Response: `200 OK`
```json
[
  {
    "id": "string",
    "reportType": "Service",
    "startDate": "2026-08-01",
    "endDate": "2026-08-07",
    "locationId": "string",
    "createdAt": "2026-08-07T12:00:00Z"
  }
]
```

## Notifications

### `GET` /notifications
Response: `200 OK`
```json
[
  {
    "id": "string",
    "message": "string",
    "orderId": "string",
    "reservationId": "string",
    "createdAt": "2026-08-07T12:00:00Z",
    "isRead": false
  }
]
```

### `PATCH` /notifications/{id}/read
Response: `200 OK`
```json
"Notification marked as read"
```

## Group & Guest Reservations

### `GET` /guest/invites/{token}
Response: `200 OK`
```json
{
  "groupReservationId": "string",
  "restaurantName": "string",
  "locationAddress": "string",
  "startTime": "2026-08-07T19:00:00Z"
}
```

### `POST` /guest/sessions
Request body:
```json
{
  "groupReservationId": "string",
  "guestName": "string"
}
```
Response: 201 Created
```json
{
  "sessionToken": "string"
}
```

### `GET` /guest/sessions/{guestSessionId}
Response: `200 OK`
```json
{
  "id": "string",
  "groupReservationId": "string",
  "guestName": "string",
  "status": "string"
}
```
### `POST` /guest-sessions/{guestSessionId}/dishes
Request body:
```json
{
  "dishId": "string",
  "quantity": 1
}
```
Response: 200 OK
```json
{
  "message": "Dish added to guest session successfully."
}
```

### `DELETE` /guest-sessions/{guestSessionId}/dishes
Request body:
```json
{
  "dishId": "string"
}
```
Response: 200 OK
```json
{
  "message": "Dish removed from guest session successfully."
}
```

### `GET` /guest/sessions/{guestSessionId}/menu
Response: 200 OK
```json
[
  {
    "id": "string",
    "name": "string",
    "imageUrl": "string",
    "price": 12.34,
    "state": "Available",
    "weight": "string",
    "dishType": "string",
    "description": "string",
    "calories": "string",
    "proteins": "string",
    "fats": "string",
    "carbohydrates": "string",
    "vitamins": "string"
  }
]
```

### `POST` /guest/sessions/{guestSessionId}/submit
Response: 200 OK
```json
{
  "message": "Guest cart submitted successfully."
}
```

### `GET` /group-reservations/{groupReservationId}/status
Response: 200 OK
```json
{
  "status": "string",
  "totalGuests": 4,
  "submittedGuests": 3
}
```

### `POST` /group-reservations/{groupReservationId}/submit
Response: 200 OK
```json
{
  "message": "Group reservation pre-order submitted successfully."
}
```

### `GET` /guest/groups/{groupReservationId}/sessions
Response: 200 OK
```json
[
  {
    "id": "string",
    "groupReservationId": "string",
    "guestName": "string",
    "status": "string"
  }
]
```

### `DELETE` /guest/groups/{groupReservationId}/sessions/{guestSessionId}
Response: 200 OK
```json
{
  "message": "Guest session removed from group successfully."
}
```