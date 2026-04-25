module OxpeckerApi.Program

open System
open System.Collections.Generic
open System.Threading.Tasks
open Microsoft.AspNetCore.Builder
open Microsoft.Extensions.DependencyInjection
open Oxpecker
open Oxpecker.OpenApi
open Scalar.AspNetCore
open OxpeckerApi.Models
open OxpeckerApi.Handlers
open OxpeckerApi.OpenApi

let endpoints store = [
    GET [
        route "/todos" (getTodos store)
        |> addOpenApi (OpenApiConfig(
            responseBodies = [| ResponseBody typeof<TodoItem array> |],
            configureOperation = fun op _ _ ->
                op.Summary <- "List all todos"
                op.Description <- "Returns every todo item in the store."
                Task.CompletedTask
        ))

        routef "/todos/{%O:guid}" (getTodo store)
        |> addOpenApi (OpenApiConfig(
            responseBodies = [|
                ResponseBody typeof<TodoItem>
                ResponseBody(typeof<ApiError>, statusCode = 404)
            |],
            configureOperation = fun op _ _ ->
                op.Summary <- "Get a todo by ID"
                op.Description <- "Returns a single todo item, or 404 if not found."
                Task.CompletedTask
        ))
    ]

    POST [
        route "/todos" (createTodo store)
        |> addOpenApi (OpenApiConfig(
            requestBody = RequestBody typeof<CreateTodoRequest>,
            responseBodies = [|
                ResponseBody(typeof<TodoItem>, statusCode = 201)
                ResponseBody(typeof<ApiError>, statusCode = 400)
            |],
            configureOperation = fun op _ _ ->
                op.Summary <- "Create a todo"
                op.Description <- "Creates a new todo item and returns it with status 201."
                Task.CompletedTask
        ))
    ]

    PUT [
        routef "/todos/{%O:guid}" (updateTodo store)
        |> addOpenApi (OpenApiConfig(
            requestBody = RequestBody typeof<UpdateTodoRequest>,
            responseBodies = [|
                ResponseBody typeof<TodoItem>
                ResponseBody(typeof<ApiError>, statusCode = 400)
                ResponseBody(typeof<ApiError>, statusCode = 404)
            |],
            configureOperation = fun op _ _ ->
                op.Summary <- "Update a todo"
                op.Description <- "Replaces the title and completed flag of an existing todo."
                Task.CompletedTask
        ))
    ]

    DELETE [
        routef "/todos/{%O:guid}" (deleteTodo store)
        |> addOpenApi (OpenApiConfig(
            responseBodies = [|
                ResponseBody(typeof<unit>, statusCode = 204)
                ResponseBody(typeof<ApiError>, statusCode = 404)
            |],
            configureOperation = fun op _ _ ->
                op.Summary <- "Delete a todo"
                op.Description <- "Permanently removes a todo. Returns 204 on success."
                Task.CompletedTask
        ))
    ]
]

[<EntryPoint>]
let main args =
    let builder = WebApplication.CreateBuilder args

    builder.Services
        .AddRouting()
        .AddOxpecker()
        .AddOpenApi(fun options ->
            options.AddSchemaTransformer<FSharpOptionSchemaTransformer>() |> ignore
            options.AddSchemaTransformer<FSharpRecordSchemaTransformer>() |> ignore
        )
    |> ignore

    let app = builder.Build()

    app.MapOpenApi() |> ignore

    app.MapScalarApiReference(fun opts ->
        opts.WithTitle("Oxpecker Todo API")
            .WithTheme(ScalarTheme.DeepSpace)
            .WithDefaultHttpClient(ScalarTarget.CSharp, ScalarClient.HttpClient)
        |> ignore
    )
    |> ignore

    let store = Dictionary<Guid, TodoItem>()

    app.UseRouting() |> ignore
    app.UseOxpecker (endpoints store) |> ignore
    app.Run()
    0
