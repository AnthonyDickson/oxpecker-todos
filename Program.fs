module OxpeckerApi.Program

open Microsoft.AspNetCore.Authentication
open Microsoft.AspNetCore.Builder
open Microsoft.Extensions.DependencyInjection
open Microsoft.OpenApi
open Oxpecker
open OxpeckerApi.Auth
open OxpeckerApi.Handlers
open OxpeckerApi.Models
open OxpeckerApi.OpenApi
open Oxpecker.OpenApi
open Scalar.AspNetCore
open System
open System.Collections.Generic
open System.Threading.Tasks

let private bearerRequirement () : OpenApiSecurityRequirement =
    let schemeRef =
        OpenApiSecuritySchemeReference ("bearerAuth", null, "SecuritySchemes")

    let requirement = OpenApiSecurityRequirement ()
    requirement[schemeRef] <- ResizeArray<string> ()
    requirement

let endpoints (store : Store) : Endpoint list = [
    GET [
        route "/todos" (getTodos store)
        |> addOpenApi (
            OpenApiConfig (
                responseBodies = [| ResponseBody typeof<TodoItem array> |],
                configureOperation =
                    fun op _ _ ->
                        op.Summary <- "List all todos"
                        op.Description <- "Returns every todo item in the store."
                        Task.CompletedTask
            )
        )

        routef "/todos/{%O:guid}" (getTodo store)
        |> addOpenApi (
            OpenApiConfig (
                responseBodies = [|
                    ResponseBody typeof<TodoItem>
                    ResponseBody (typeof<ApiError>, statusCode = 404)
                |],
                configureOperation =
                    fun op _ _ ->
                        op.Summary <- "Get a todo by ID"
                        op.Description <- "Returns a single todo item, or 404 if not found."
                        Task.CompletedTask
            )
        )

        route "/private-todos" (requireAuthenticated >=> getPrivateTodos store)
        |> addOpenApi (
            OpenApiConfig (
                responseBodies = [|
                    ResponseBody typeof<TodoItem array>
                    ResponseBody (typeof<ApiError>, statusCode = 401)
                |],
                configureOperation =
                    fun op _ _ ->
                        op.Summary <- "List private todos"
                        op.Description <- $"Protected demo route. Use Authorization: Bearer {DemoToken}"
                        op.Security <- ResizeArray [ bearerRequirement () ]
                        Task.CompletedTask
            )
        )
    ]

    POST [
        route "/todos" (createTodo store)
        |> addOpenApi (
            OpenApiConfig (
                requestBody = RequestBody typeof<CreateTodoRequest>,
                responseBodies = [|
                    ResponseBody (typeof<TodoItem>, statusCode = 201)
                    ResponseBody (typeof<ApiError>, statusCode = 400)
                |],
                configureOperation =
                    fun op _ _ ->
                        op.Summary <- "Create a todo"
                        op.Description <- "Creates a new todo item and returns it with status 201."
                        Task.CompletedTask
            )
        )
    ]

    PUT [
        routef "/todos/{%O:guid}" (updateTodo store)
        |> addOpenApi (
            OpenApiConfig (
                requestBody = RequestBody typeof<UpdateTodoRequest>,
                responseBodies = [|
                    ResponseBody typeof<TodoItem>
                    ResponseBody (typeof<ApiError>, statusCode = 400)
                    ResponseBody (typeof<ApiError>, statusCode = 404)
                |],
                configureOperation =
                    fun op _ _ ->
                        op.Summary <- "Update a todo"
                        op.Description <- "Replaces the title and completed flag of an existing todo."
                        Task.CompletedTask
            )
        )
    ]

    DELETE [
        routef "/todos/{%O:guid}" (deleteTodo store)
        |> addOpenApi (
            OpenApiConfig (
                responseBodies = [|
                    ResponseBody (typeof<unit>, statusCode = 204)
                    ResponseBody (typeof<ApiError>, statusCode = 404)
                |],
                configureOperation =
                    fun op _ _ ->
                        op.Summary <- "Delete a todo"
                        op.Description <- "Permanently removes a todo. Returns 204 on success."
                        Task.CompletedTask
            )
        )
    ]
]

[<EntryPoint>]
let main (args : string array) : int =
    let builder = WebApplication.CreateBuilder args

    builder.Services
        .AddAuthentication(DemoScheme)
        .AddScheme<AuthenticationSchemeOptions, DemoBearerAuthHandler>(DemoScheme, fun _ -> ())
        .Services.AddAuthorization()
        .AddRouting()
        .AddOxpecker()
        .AddOpenApi (fun options ->
            options.AddSchemaTransformer<FSharpOptionSchemaTransformer> () |> ignore
            options.AddSchemaTransformer<FSharpRecordSchemaTransformer> () |> ignore

            options.AddDocumentTransformer (fun doc _ _ ->
                if isNull doc.Components then
                    doc.Components <- OpenApiComponents ()

                if isNull doc.Components.SecuritySchemes then
                    doc.Components.SecuritySchemes <- Dictionary<string, IOpenApiSecurityScheme> ()

                doc.Components.SecuritySchemes["bearerAuth"] <-
                    OpenApiSecurityScheme (
                        Type = SecuritySchemeType.Http,
                        Scheme = "bearer",
                        BearerFormat = "JWT",
                        Description = $"Demo bearer token. Use `{DemoToken}`."
                    )

                Task.CompletedTask)
            |> ignore)
    |> ignore

    let app = builder.Build ()

    app.MapOpenApi () |> ignore

    app.MapScalarApiReference (fun opts ->
        opts
            .WithTitle("Oxpecker Todo API")
            .WithTheme(ScalarTheme.DeepSpace)
            .WithDefaultHttpClient (ScalarTarget.CSharp, ScalarClient.HttpClient)
        |> ignore)
    |> ignore

    let store = Dictionary<Guid, TodoItem> ()

    app.UseRouting () |> ignore
    app.UseAuthentication () |> ignore
    app.UseAuthorization () |> ignore
    app.UseOxpecker (endpoints store) |> ignore
    app.Run ()
    0
