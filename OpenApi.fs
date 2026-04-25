module OxpeckerApi.OpenApi

open Microsoft.AspNetCore.OpenApi
open Microsoft.FSharp.Reflection
open Microsoft.OpenApi
open System.Collections.Generic
open System.Threading
open System.Threading.Tasks

let private isOptionType (t: System.Type) =
    if t.IsGenericType then
        let definition = t.GetGenericTypeDefinition()
        definition = typedefof<option<_>> || definition = typedefof<voption<_>>
    else
        false

type FSharpRecordSchemaTransformer() =
    interface IOpenApiSchemaTransformer with
        member _.TransformAsync(schema, context, _cancellationToken: CancellationToken) =
            let jsonType = context.JsonTypeInfo.Type

            if FSharpType.IsRecord jsonType then
                let required =
                    jsonType
                    |> FSharpType.GetRecordFields
                    |> Seq.filter (fun field -> not (isOptionType field.PropertyType))
                    |> Seq.map (fun field -> field.Name)
                    |> HashSet<string>

                if required.Count > 0 then
                    schema.Required <- required

            if
                not (isNull context.JsonPropertyInfo)
                && context.JsonPropertyInfo.PropertyType = typeof<string>
            then
                schema.Type <- JsonSchemaType.String
                if not (isNull schema.OneOf) then
                    schema.OneOf.Clear()

                if not (isNull schema.AnyOf) then
                    schema.AnyOf.Clear()

                if not (isNull schema.AllOf) then
                    schema.AllOf.Clear()

            Task.CompletedTask
