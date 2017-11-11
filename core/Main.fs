﻿module Rewrap.Core

open Extensions


let mutable private lastDocState : DocState = 
    { filePath = ""; language = ""; version = 0; selections = [||] }

let private docWrappingColumns =
    new System.Collections.Generic.Dictionary<string, int>()

let private columnFromColumns (docState: DocState) (rulers: int[]) : int =
    let filePath = docState.filePath

    if rulers.Length = 0 then
        80
    else if rulers.Length = 1 then
        rulers.[0]
    else
        if not (docWrappingColumns.ContainsKey(filePath)) then
            docWrappingColumns.[filePath] <- rulers.[0]

        else if docState = lastDocState then
            let nextRulerIndex =
                rulers
                    |> Array.tryFindIndex ((=) docWrappingColumns.[filePath])
                    |> Option.map (fun i -> (i + 1) % rulers.Length)
                    |> Option.defaultValue 0

            docWrappingColumns.[filePath] <- rulers.[nextRulerIndex]

        docWrappingColumns.[filePath]
 

let saveDocState docState =
    lastDocState <- docState

let findLanguage name filePath : string =
    Parsing.Documents.findLanguage name filePath
        |> Option.map (fun l -> l.name)
        |> Option.defaultValue null


let languages : string[] =
    Parsing.Documents.languages
        |> Array.map (fun l -> l.name)



let rewrap
    (docState: DocState)
    (settings: Settings)
    (lines: seq<string>) =
    
    let parser = 
        Parsing.Documents.select docState.language docState.filePath

    let originalLines =
        List.ofSeq lines |> Nonempty.fromListUnsafe

    let newSettings =
        { settings with column = columnFromColumns docState settings.columns }

    originalLines
        |> parser settings
        |> Selections.wrapSelected 
            originalLines (List.ofSeq docState.selections) newSettings