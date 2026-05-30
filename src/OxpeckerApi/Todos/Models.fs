module OxpeckerApi.Todos.Models

open System

// ── Domain types ──────────────────────────────────────────────────────────────

type TodoItem = {
    Id : Guid
    Title : string
    Completed : bool
    CreatedAt : DateTime
}

// ── Request DTOs ─────────────────────────────────────────────────────────────

type CreateTodoRequest = { Title : string }

type UpdateTodoRequest = { Title : string; Completed : bool }

// ── Error DTO ────────────────────────────────────────────────────────────────

type ApiError = { Error : string; Details : string }
