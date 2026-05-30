module OxpeckerApi.Todos.Handlers

open System
open Oxpecker
open OxpeckerApi.Todos.Models
open OxpeckerApi.Todos
open OxpeckerApi.Middleware

/// GET /todos — list all items
let getTodos (store : Store.t) : EndpointHandler =
    fun ctx ->
        task {
            let! items = Store.getAll store
            return! ctx.WriteJson items
        }

/// GET /todos/{id} — get one item
let getTodo (store : Store.t) (id : Guid) : EndpointHandler =
    fun ctx ->
        task {
            let! todo = Store.get store id

            match todo with
            | Some item -> return! ctx.WriteJson item
            | None -> return! notFound $"Todo {id} not found" ctx
        }

/// GET /private-todos — protected demo route
let getPrivateTodos (store : Store.t) : EndpointHandler =
    fun ctx ->
        task {
            let! items = Store.getAll store
            return! ctx.WriteJson items
        }

/// POST /todos — create an item
let createTodo (store : Store.t) : EndpointHandler =
    fun ctx ->
        task {
            let! req = ctx.BindJson<CreateTodoRequest> ()

            if String.IsNullOrWhiteSpace req.Title then
                ctx.SetStatusCode 400

                return!
                    ctx.WriteJson {
                        Error = "Validation Error"
                        Details = "Title is required"
                    }
            else
                let item = {
                    Id = Guid.NewGuid ()
                    Title = req.Title.Trim ()
                    Completed = false
                    CreatedAt = DateTime.UtcNow
                }

                Store.upsert store item
                ctx.SetStatusCode 201
                return! ctx.WriteJson item
        }

/// PUT /todos/{id} — replace an item
let updateTodo (store : Store.t) (id : Guid) : EndpointHandler =
    fun ctx ->
        task {
            let! req = ctx.BindJson<UpdateTodoRequest> ()

            if String.IsNullOrWhiteSpace req.Title then
                ctx.SetStatusCode 400

                return!
                    ctx.WriteJson {
                        Error = "Validation Error"
                        Details = "Title is required"
                    }
            else
                let! updated = Store.update store id (req.Title.Trim ()) req.Completed

                match updated with
                | Some updated -> return! ctx.WriteJson updated
                | None -> return! notFound $"Todo {id} not found" ctx
        }

/// DELETE /todos/{id} — remove an item
let deleteTodo (store : Store.t) (id : Guid) : EndpointHandler =
    fun ctx ->
        task {
            let! deleted = Store.delete store id

            if deleted then
                ctx.SetStatusCode 204
                return ()
            else
                return! notFound $"Todo {id} not found" ctx
        }
