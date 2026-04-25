module OxpeckerApi.Handlers

open Microsoft.AspNetCore.Http
open Oxpecker
open OxpeckerApi.Auth
open OxpeckerApi.Models
open System
open System.Collections.Generic

let private notFound msg : EndpointHandler =
    fun ctx ->
        ctx.SetStatusCode 404
        ctx.WriteJson { Error = "Not Found"; Details = msg }

let requireAuthenticated : EndpointMiddleware =
    fun next ctx ->
        task {
            if
                not (isNull ctx.User)
                && not (isNull ctx.User.Identity)
                && ctx.User.Identity.IsAuthenticated
            then
                return! next ctx
            else
                ctx.SetStatusCode 401
                return!
                    ctx.WriteJson {
                        Error = "Unauthorized"
                        Details = $"Provide Authorization: Bearer {DemoToken}"
                    }
        }

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

/// GET /private-todos — protected demo route
let getPrivateTodos (store: Store) : EndpointHandler =
    fun ctx ->
        let items = store.Values |> Seq.toArray
        ctx.WriteJson items

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
