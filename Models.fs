module OxpeckerApi.Models

open System

// ── Domain types ──────────────────────────────────────────────────────────────

type TodoItem = {
    Id        : Guid
    Title     : string
    Completed : bool
    CreatedAt : DateTime
}

type CreateTodoRequest = {
    Title : string
}

type UpdateTodoRequest = {
    Title     : string
    Completed : bool
}

type ApiError = {
    Error   : string
    Details : string
}
