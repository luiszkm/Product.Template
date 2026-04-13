# AI Controller

Controller: `AiController`

Base route: `/api/v1/ai`

## POST `/api/v1/ai/chat`

- Policy: `Authenticated`
- Status: `200`, `400`, `401`

### Request body (`ChatCommand`)

```json
{
  "message": "string",
  "history": [
    {
      "role": "string",
      "content": "string",
      "toolCallId": "string|null"
    }
  ]
}
```

Campos:
- `message` (string, obrigatorio)
- `history` (array de `LlmMessage`, opcional)
  - `role` (string)
  - `content` (string)
  - `toolCallId` (string|null)

### Response 200 (`ChatOutput`)

```json
{
  "reply": "string",
  "iterationsUsed": 1
}
```

Campos:
- `reply` (string)
- `iterationsUsed` (int)

