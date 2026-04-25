module OxpeckerApi.Handlers

open System
open System.Collections.Generic
open Microsoft.AspNetCore.Http
open Oxpecker
open OxpeckerApi.Models

// ── In-memory store (replace with a real DB in production) ───────────────────

let private notFound msg : EndpointHandler =
    fun ctx ->
        ctx.SetStatusCode 404
        ctx.WriteJson { Error = "Not Found"; Details = msg }

// ── Handlers ─────────────────────────────────────────────────────────────────

/// GET /todos — list all items
let getTodos (store: Store) : EndpointHandler =
    fun ctx ->
        let items = store.Values |> Seq.toArray
        ctx.WriteJson items

/// GET /todos/{id} — get one item
let getTodo (store: Store) (id: Guid) : EndpointHandler =
    fun ctx ->
        match store.TryGetValue id with
        | true, item -> ctx.WriteJson item
        | _          -> notFound $"Todo {id} not found" ctx

/// POST /todos — create an item
let createTodo (store: Store): EndpointHandler =
    fun ctx -> task {
        let! req = ctx.BindJson<CreateTodoRequest>()

        if String.IsNullOrWhiteSpace req.Title then
            ctx.SetStatusCode 400
            return! ctx.WriteJson { Error = "Validation Error"; Details = "Title is required" }
        else
            let item = {
                Id        = Guid.NewGuid()
                Title     = req.Title.Trim()
                Completed = false
                CreatedAt = DateTime.UtcNow
            }
            store[item.Id] <- item
            ctx.SetStatusCode 201
            return! ctx.WriteJson item
    }

/// PUT /todos/{id} — replace an item
let updateTodo (store: Store)(id: Guid) : EndpointHandler =
    fun ctx -> task {
        match store.TryGetValue id with
        | false, _ ->
            return! notFound $"Todo {id} not found" ctx
        | true, existing ->
            let! req = ctx.BindJson<UpdateTodoRequest>()

            if String.IsNullOrWhiteSpace req.Title then
                ctx.SetStatusCode 400
                return! ctx.WriteJson { Error = "Validation Error"; Details = "Title is required" }
            else
                let updated = { existing with Title = req.Title.Trim(); Completed = req.Completed }
                store[id] <- updated
                return! ctx.WriteJson updated
    }

/// DELETE /todos/{id} — remove an item
let deleteTodo (store: Store)(id: Guid) : EndpointHandler =
    fun ctx ->
        if store.Remove id then
            ctx.SetStatusCode 204
            ctx.WriteText ""
        else
            notFound $"Todo {id} not found" ctx
