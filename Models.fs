module OxpeckerApi.Models

open System
open System.Collections.Generic

// ── Domain types ──────────────────────────────────────────────────────────────

type TodoItem = {
    Id : Guid
    Title : string
    Completed : bool
    CreatedAt : DateTime
}
