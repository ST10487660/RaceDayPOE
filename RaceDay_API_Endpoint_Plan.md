# RaceDay Part 1 — API Endpoint Plan

## Purpose

This document defines the REST API that will be implemented in Part 2. The implementation should closely follow this plan so that the approved database design, API and later MVC application remain consistent.

### Roles
## Authentication

RaceDay will use authentication to identify users before allowing access to protected API endpoints. After a successful login, the system will use the authenticated user's role to determine which operations they are allowed to perform.

- Organisers can manage events, categories, enrolments and participant results.
- Participants can browse events, enrol in event categories and view their own results.
- Requests to protected endpoints without authentication will return `401 Unauthorized`.
- Authenticated users who attempt to access functionality outside their role will receive `403 Forbidden`.

- **Organiser:** creates, edits and deletes events; manages categories; captures participant results; views event enrolments.
- **Participant:** creates an account; browses events; enters an event by selecting a category; views own enrolments; tracks personal results.
- ## HTTP Status Codes

The RaceDay API will use standard HTTP status codes to clearly indicate the result of each request.

- `200 OK` - The request was successful.
- `201 Created` - A new resource was created successfully.
- `400 Bad Request` - The request contains invalid or missing data.
- `401 Unauthorized` - Authentication is required.
- `403 Forbidden` - The authenticated user does not have permission.
- `404 Not Found` - The requested resource could not be found.
- `409 Conflict` - The request conflicts with existing data, such as a duplicate enrolment.
- `500 Internal Server Error` - An unexpected server error occurred.

- ## Data Format

The RaceDay API will use JSON for communication between the client and server.

All request bodies containing data will be sent using the `application/json` content type. API responses will also return data in JSON format to ensure consistency between the MVC application and the REST API.

Example event response:

{
  "eventId": 1,
  "eventName": "Johannesburg City Run",
  "eventDate": "2026-10-10",
  "location": "Johannesburg",
  "maxParticipants": 500
}

## Input Validation

The RaceDay API will validate incoming data before processing a request.

- Required fields must contain values.
- Email addresses must use a valid format.
- Event names and locations cannot be blank.
- Maximum participants must be greater than zero.
- Users can only have the `Organiser` or `Participant` role.
- Duplicate event enrolments will not be allowed.
- Invalid input will return `400 Bad Request` with an appropriate error message.

## API Naming Conventions

The RaceDay API will follow consistent naming conventions to make the endpoints easy to understand and maintain.

- Endpoint routes will use lowercase resource names.
- Plural nouns will be used for resources such as `/api/events`, `/api/categories` and `/api/results`.
- HTTP methods will describe the operation instead of including actions such as "create" or "delete" in the route.
- Route parameters will identify specific resources, for example `/api/events/{id}`.
- JSON property names will use camelCase.
  
## Endpoint Table

| HTTP Method | Route | Description | Role Required | Request Body | Expected Response |
|---|---|---|---|---|---|
| POST | `/api/auth/register` | Creates a new participant account. | Public | `{ fullName, email, password }` | `201 Created` with user ID/profile; `400` validation error; `409` email exists |
| POST | `/api/auth/login` | Authenticates a user and returns an access token. | Public | `{ email, password }` | `200 OK` with token and role; `401` invalid credentials |
| GET | `/api/users/me` | Returns the profile of the authenticated user. | Any logged-in user | None | `200 OK`; `401` unauthenticated |
| PUT | `/api/users/me` | Updates the authenticated user's profile. | Any logged-in user | `{ fullName, email }` | `200 OK`; `400` validation; `409` email exists |
| GET | `/api/events` | Lists upcoming RaceDay events. | Public | None | `200 OK` with event list |
| GET | `/api/events/{id}` | Returns details for one event, including its categories and route. | Public | None | `200 OK`; `404` event not found |
| POST | `/api/events` | Creates a new event. | Organiser | `{ eventName, description, eventDate, location, maxParticipants, categoryIds }` | `201 Created`; `400` invalid data; `403` forbidden |
| PUT | `/api/events/{id}` | Updates an existing event. | Organiser | Event fields to update | `200 OK`; `404`; `403`; `400` |
| DELETE | `/api/events/{id}` | Deletes an event that is no longer needed. | Organiser | None | `204 No Content`; `404`; `403`; `409` if related records prevent deletion |
| GET | `/api/categories` | Lists all event categories. | Public | None | `200 OK` with categories |
| POST | `/api/categories` | Creates a new event category. | Organiser | `{ categoryName, description }` | `201 Created`; `400`; `409` duplicate |
| PUT | `/api/categories/{id}` | Updates a category. | Organiser | `{ categoryName, description }` | `200 OK`; `404`; `403` |
| DELETE | `/api/categories/{id}` | Removes a category not in active use. | Organiser | None | `204 No Content`; `404`; `409` in-use category |
| GET | `/api/events/{eventId}/categories` | Returns categories available for a specific event. | Public | None | `200 OK`; `404` event not found |
| POST | `/api/events/{eventId}/enrolments` | Enrols the logged-in participant in an event using a selected category. | Participant | `{ categoryId }` | `201 Created`; `400` invalid category; `404`; `409` already enrolled/full |
| GET | `/api/enrolments/me` | Lists the logged-in participant's own enrolments. | Participant | None | `200 OK` with enrolments |
| GET | `/api/events/{eventId}/enrolments` | Views all enrolments for an event. | Organiser | None | `200 OK`; `403`; `404` |
| DELETE | `/api/enrolments/{id}` | Cancels the authenticated participant's enrolment. | Participant | None | `204 No Content`; `404`; `403` |
| GET | `/api/results/me` | Returns the authenticated participant's personal result history. | Participant | None | `200 OK` with results |
| GET | `/api/events/{eventId}/results` | Lists all results for an event. | Organiser | None | `200 OK`; `403`; `404` |
| POST | `/api/events/{eventId}/results` | Captures a participant result for an event. | Organiser | `{ enrolmentId, position, finishTime, distanceKm }` | `201 Created`; `400`; `404`; `409` if result exists |
| PUT | `/api/results/{id}` | Corrects an existing participant result. | Organiser | `{ position, finishTime, distanceKm }` | `200 OK`; `404`; `403` |
| DELETE | `/api/results/{id}` | Removes an incorrect result. | Organiser | None | `204 No Content`; `404`; `403` |
| GET | `/api/events/{eventId}/route` | Returns route information used to prepare for race day. | Public | None | `200 OK`; `404` |
| GET | `/api/events/{eventId}/weather` | Returns latest weather information for an event. | Public | None | `200 OK`; `404` |

## Access-Control Rules

1. Authentication endpoints are public.
2. Event and category browsing is public.
3. Organiser-only operations must reject Participants with `403 Forbidden`.
4. Participant-only enrolment and personal-history operations must reject Organisers where the operation is participant-specific.
5. `/api/users/me`, `/api/enrolments/me`, and `/api/results/me` must only return the authenticated user's own data.
6. An organiser may only manage event data through authenticated organiser endpoints.
7. Part 2 should use role-based authorization consistently with these rules.

## Request/Response Conventions

- JSON is used for request and response bodies.
- IDs are integer values.
- Dates use ISO format such as `2026-10-18`.
- HTTP status codes should accurately describe the outcome.
- Validation failures should return `400 Bad Request`.
- Missing resources should return `404 Not Found`.
- Authentication/authorization failures should use `401 Unauthorized` or `403 Forbidden` as appropriate.
- Duplicate enrolments or conflicting resources should use `409 Conflict`.

## Mapping to the Database

The planned API is based on the following entities:

`Users`, `Events`, `Categories`, `EventCategories`, `Routes`, `Enrolments`, `Results`, and `Weather`.

The SQL script and ERD in this `/docs` folder are designed to match these entities and relationships.
