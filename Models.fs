module OxpeckerApi.Models

open System

// ── Domain types ──────────────────────────────────────────────────────────────

type TodoItem = {
    Id : Guid
    Title : string
    Completed : bool
    CreatedAt : DateTime
}
