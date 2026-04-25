module OxpeckerApi.Models

open System
open System.Collections.Generic

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

type Store = Dictionary<Guid, TodoItem>
