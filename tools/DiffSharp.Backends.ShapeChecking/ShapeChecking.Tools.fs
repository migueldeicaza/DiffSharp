﻿namespace rec DiffSharp.ShapeChecking

open System
open System.Reflection
open DiffSharp
open DiffSharp.Model
open DiffSharp.ShapeChecking
open DiffSharp.Backends.ShapeChecking
open System.Runtime.CompilerServices

[<AutoOpen>]
module Tools =

    /// Record a stack of ranges in an exception. This uses exactly the same protocol as FsLive
    type System.Exception with 
        member e.EvalLocationStack 
            with get() = 
                if e.Data.Contains "location" then 
                    match e.Data.["location"] with 
                    | :? ((string * int * int * int * int)[]) as stack -> stack
                    | _ -> [| |]
                else
                    [| |]
            and set (data : (string * int * int * int * int)[]) = 
                e.Data.["location"] <- data

    let DiagnosticFromException  (loc: SourceLocation) (err: exn) =
        let stack = [| for (f,sl,sc,el,ec) in err.EvalLocationStack -> { File=f;StartLine=sl;StartColumn=sc;EndLine=el;EndColumn=ec }  |]
        { Severity=2; 
          Number = 1001
          Message = err.Message
          LocationStack = Array.append [| loc |] stack }

    let (|Integer|_|) toks = 
        match toks with 
        | Choice1Of3 c :: rest -> Some (c, rest)
        | _ -> None
    let (|Ident|_|) toks = 
        match toks with 
        | Choice3Of3 c :: rest -> Some (c, rest)
        | _ -> None
    let (|Symbol|_|) toks = 
        match toks with 
        | Choice2Of3 c :: rest -> Some (c, rest)
        | _ -> None

    type ParserLogic(syms: SymScope, loc) =
        //printfn "making symbolic for model parameter %s, givenArgInfo = %A" p.Name givenArgInfo
        let tokenize (text: string) = 
            [ let mutable i = 0 
              while i < text.Length do
                    if Char.IsDigit (text, i) then
                        let start = i
                        while i < text.Length && (Char.IsDigit (text, i)) do
                            i <- i + 1
                        yield Choice1Of3 (System.Int32.Parse text.[start..i-1])
                    elif text.[i] = '+' || 
                         text.[i] = '/' || 
                         text.[i] = '*' || 
                         text.[i] = '×' || // OK, I like unicode
                         text.[i] = '-' || 
                         text.[i] = '[' || 
                         text.[i] = ']' || 
                         text.[i] = ',' || 
                         text.[i] = '(' || 
                         text.[i] = ')' then
                        let tok = text.[i..i]
                        i <- i + tok.Length
                        yield Choice2Of3 tok
                    elif Char.IsLetter (text, i) || Char.IsSymbol (text, i) then
                        let start = i
                        while i < text.Length  && (Char.IsLetter (text, i) || Char.IsSymbol (text, i) || Char.IsDigit (text, i)) do
                            if Char.IsSurrogatePair(text, i) then 
                                i <- i + 2
                            else
                                i <- i + 1
                        yield Choice3Of3 text.[start..i-1]
                    elif Char.IsWhiteSpace (text, i) then
                        i <- i + 1
                    else  
                        failwithf "%O: unknown character '%c' in expression" loc text.[i] ]
        let rec (|Expr|_|) toks = 
            match toks with 
            | DivExpr (e1, Symbol("+", Expr (e2, rest))) -> Some (syms.Create("add", [| e1; e2 |]), rest)
            | DivExpr (e1, Symbol("-", Expr (e2, rest))) -> Some (syms.Create("sub", [| e1; e2 |]), rest)
            | DivExpr (e, rest) -> Some (e, rest)
            | _ -> None
        and (|DivExpr|_|) toks = 
            match toks with 
            | MulExpr (e1, Symbol("/", MulExpr (e2, rest))) -> Some (syms.Create("div", [| e1; e2 |]), rest)
            | MulExpr (e, rest) -> Some (e, rest)
            | _ -> None
        and (|MulExpr|_|) toks = 
            match toks with 
            | AtomExpr (e1, Symbol("*", MulExpr (e2, rest))) -> Some (syms.Create("mul", [| e1; e2 |]), rest)
            | AtomExpr (e, rest) -> Some (e, rest)
            | _ -> None
        and (|AtomExpr|_|) toks = 
            match toks with 
            | Symbol ("(", Expr (e, Symbol (")", rest))) -> Some (e, rest)
            | Integer (n, rest) -> Some (syms.CreateConst n, rest)
            | Ident (n, rest) -> Some (syms.CreateVar (n, loc), rest)
            | _ -> None
        and (|IntExprs|_|) toks = 
            match toks with 
            | Expr (e, Symbol (("," | "×"), IntExprs (es, rest))) -> Some (e :: es, rest)
            | Expr (e, rest) -> Some ([e], rest)
            | _ -> None
        and (|ShapeExpr|_|) toks = 
            match toks with 
            | Symbol ("[", IntExprs (es, Symbol ("]", rest))) -> Some (Shape (Array.map Int.FromSymbol (Array.ofList es)), rest)
            | IntExprs (es, rest) -> Some (Shape (Array.map Int.FromSymbol (Array.ofList es)), rest)
            | _ -> None

        let parseSymbolicIntArg (text: string) (loc: SourceLocation) : Int =
            let toks = tokenize text
            let sym = 
                match toks with 
                | Expr (e, []) -> e
                | _ -> failwithf "%O: invalid expression %s" loc text
            Int.FromSymbol sym

        let parseSymbolicShapeArg (text: string) (loc: SourceLocation) : Shape =
            let toks = tokenize text
            match toks with 
            | ShapeExpr (e, []) -> e
            | _ -> failwithf "%O: invalid shape %s" loc text

        let getSymbolicIntArg (givenArgInfo: (obj * SourceLocation) option) (p: ParameterInfo) (loc: SourceLocation) : Int =
            //printfn "making symbolic for model parameter %s, givenArgInfo = %A" p.Name givenArgInfo
            match givenArgInfo with 
            | Some (:? int as n, _) -> Int n 
            | Some (:? string as nm, loc) -> parseSymbolicIntArg nm loc
            | Some (arg, loc) -> failwithf "%O: unknown arg specification %A" loc arg
            | None -> syms.CreateIntVar(p.Name, loc) 

        let getSymbolicShapeArg (givenArgInfo: (obj * SourceLocation) option) (_p: ParameterInfo) (loc: SourceLocation) : Shape =
            //printfn "making symbolic for model parameter %s" p.Name
            match givenArgInfo with 
            | Some (:? int as n, _) -> Shape [| n |]
            | Some (:? string as nm, loc) -> parseSymbolicShapeArg nm loc
            | Some (:? (obj[]) as nms, loc) -> 
                Shape [| for nm in nms -> 
                            match nm with 
                            | :? int as n -> Int n
                            | :? string as nm -> parseSymbolicIntArg nm loc 
                            | arg -> failwithf "%O: unknown arg specification %A" loc arg |]
            | Some (arg, loc) -> failwithf "%O: unknown arg specification %A" loc arg
            | None -> failwithf "%O: shape needs argument information in ShapeCheck attribute, e.g. [<ShapeCheck([| 1;4;2 |])>] or [<ShapeCheck([| \"N\";\"M\" |])>] " loc

        let getSymbolicTensorArg givenArgInfo (p: ParameterInfo) loc : Tensor =
            let shape = getSymbolicShapeArg givenArgInfo p loc
            dsharp.zeros(shape)

        let getSampleArg (givenArgInfo: (obj * SourceLocation) option) (p: ParameterInfo) (dflt: 'T) loc : 'T =
            //printfn "making symbolic for model parameter %s" p.Name
            match givenArgInfo with 
            | Some (:? 'T as n, _) -> n 
            | Some (arg, loc) -> failwithf "%O: unknown arg specification %A" loc arg
            | None -> 
                printfn "%O: assuming sample value '%O' for model parameter %s" loc dflt p.Name
                dflt

        member _.GetArg optionals givenArgInfo (p: ParameterInfo) loc =
            let pty = p.ParameterType
            let pts = pty.ToString()
            if pts = "DiffSharp.Int" then
                getSymbolicIntArg givenArgInfo p loc |> box
            elif pts = "DiffSharp.Shape" then
                getSymbolicShapeArg givenArgInfo p loc |> box
            elif pts = "DiffSharp.Tensor" then
                getSymbolicTensorArg givenArgInfo p loc |> box
            elif pts = "DiffSharp.ShapeChecking.ISymScope" then
                syms |> box
            elif pts = "DiffSharp.ShapeChecking.SymScope" then
                syms |> box
            elif pts = "System.Int32" then 
                getSampleArg givenArgInfo p 1 loc |> box
            elif pts = "System.Single" then 
                getSampleArg givenArgInfo p 1.0f loc |> box
            elif pts = "System.Double" then 
                getSampleArg givenArgInfo p 1.0 loc |> box
            elif pts = "System.Boolean" then 
                getSampleArg givenArgInfo p true loc |> box
            elif pts = "System.String" then 
                getSampleArg givenArgInfo p "" loc|> box
            elif optionals && pts = "Microsoft.FSharp.Core.FSharpOption`1[DiffSharp.Int]" then 
                getSymbolicIntArg givenArgInfo p loc |> Some |> box
            elif optionals && pts = "Microsoft.FSharp.Core.FSharpOption`1[DiffSharp.Shape]" then 
                getSymbolicShapeArg givenArgInfo p loc |> Some |> box
            elif optionals && pts = "Microsoft.FSharp.Core.FSharpOption`1[DiffSharp.Tensor]" then 
                getSymbolicTensorArg givenArgInfo p loc |> Some |> box
            elif optionals && pts = "Microsoft.FSharp.Core.FSharpOption`1[System.Boolean]" then 
                getSampleArg givenArgInfo p true loc |> Some |> box
            elif optionals && pts = "Microsoft.FSharp.Core.FSharpOption`1[System.Int32]" then 
                getSampleArg givenArgInfo p 1 loc |> Some |> box
            elif optionals && pts = "Microsoft.FSharp.Core.FSharpOption`1[System.Single]" then 
                getSampleArg givenArgInfo p 1.0f loc |> Some |> box
            elif optionals && pts = "Microsoft.FSharp.Core.FSharpOption`1[System.Double]" then 
                getSampleArg givenArgInfo p 1.0 loc |> Some |> box
            elif optionals && pts = "Microsoft.FSharp.Core.FSharpOption`1[System.String]" then 
                getSampleArg givenArgInfo p "" loc |> Some |> box
            elif pts.StartsWith("Microsoft.FSharp.Core.FSharpOption`1[") then 
                if optionals then 
                    printfn "%O: Optional model parameter '%s' has unknown type '%O'" loc p.Name p.ParameterType
                null // None
            else 
                failwithf "%O: Model parameter '%s' has unknown type '%O'. Consider changing the type or extending the shape checking tools to understand this type of argument" loc p.Name p.ParameterType

        member t.GetParams optionals  (ps: ParameterInfo[]) (givenArgInfos: obj[]) loc =
            [| for i in 0 .. ps.Length - 1 do 
                    let p = ps.[i]
                    let givenArgInfo = (if i < givenArgInfos.Length then Some (givenArgInfos.[i], loc) else None)
                    t.GetArg optionals givenArgInfo p loc |]

    // Constrain the return shape
    let constrainReturnValueByShapeInfo syms (retActual: obj) (shapeInfo: obj) (retParam: ParameterInfo) loc =
        match retActual, shapeInfo with 
        | null, _ | _, null ->  Ok ()
        | (:? Tensor | :? Shape | :? Int), info -> 
            let logic = ParserLogic(syms, loc) 
            let retReqd = logic.GetArg false (Some (info, loc)) retParam loc 
            match retReqd, retActual with 
            | (:? Tensor as retReqd), (:? Tensor as retActual) ->
                if not (retActual.shapex =~= retReqd.shapex) then
                    let msg = sprintf "Shape mismatch. Expected a tensor with shape '%O' but got shape '%O'" retReqd.shapex retActual.shapex
                    Error { Severity=2; LocationStack=[| loc |]; Message=msg; Number=1999 }
                else
                   Ok ()
            | (:? Shape as retReqd), (:? Shape as retActual) ->
                if not (retActual =~= retReqd) then
                    let msg = sprintf "Shape mismatch. Expected shape '%O' but got shape '%O'" retReqd retActual
                    Error { Severity=2; LocationStack=[| loc |]; Message=msg; Number=1999 }
                else
                   Ok ()
            | (:? Int as retReqd), (:? Int as retActual) ->
                if not (retActual =~= retReqd) then
                    let msg = sprintf "Shape mismatch. Expected '%O' but got '%O'" retReqd retActual
                    Error { Severity=2; LocationStack=[| loc |]; Message=msg; Number=1999 }
                else
                   Ok ()
            | _ -> Ok ()
        | _ -> 
            let msg = sprintf "Unexpected return from method with ReturnShape attribute" 
            Error { Severity=1; LocationStack=[| loc |]; Message=msg; Number=1999 }

    let makeModelAndRunShapeChecks (syms: SymScope) optionals (ctor: ConstructorInfo) ctorGivenArgs loc methLocs =
        let ctorArgs = ParserLogic(syms, loc).GetParams optionals (ctor.GetParameters()) ctorGivenArgs loc
        let model = 
           try ctor.Invoke(ctorArgs) |> Ok
           with :?TargetInvocationException as e -> Error (DiagnosticFromException loc e.InnerException)
        match model with 
        | Error e -> ctorArgs, Error e, [| |], [| e |]
        | Ok model ->

        let diags = ResizeArray()
        let methCalls = ResizeArray()
        for meth in ctor.DeclaringType.GetMethods() do
          if meth.ContainsGenericParameters || meth.DeclaringType.ContainsGenericParameters then
            let msg = "Skipping ShapeCheck for a generic method"
            let diag = { Severity=1; LocationStack=[| loc |]; Message=msg; Number=1994 }
            diags.Add diag
          else
            
            // Use a better location for the method attribute if given
            let loc =
               methLocs 
               |> Array.tryFind (fun (methName, _file, _sl, _sc, _el, _ec) -> methName = meth.Name)
               |> function 
                  | None -> loc
                  | Some (_, file, sl, sc, el, ec) -> { File=file; StartLine=sl; StartColumn=sc; EndLine=el; EndColumn=ec} 

            printfn "meth %s, loc = %O"  meth.Name loc
            for attr in meth.GetCustomAttributes(typeof<ShapeCheckAttribute>, true) do
                printfn "meth %s has attr"  meth.Name
                try 
                    syms.Push()
                    let attr = attr :?> ShapeCheckAttribute
                    let args = ParserLogic(syms, loc).GetParams optionals (meth.GetParameters()) attr.GivenArgs loc

                    let res =
                        try 
                            meth.Invoke (model, args) |> Ok
                        with :?TargetInvocationException as e -> 
                            let e = e.InnerException
                            printfn "meth %s error, res = %A" meth.Name e
                            Error (DiagnosticFromException loc e)

                    match res with
                    | Ok retActual -> 
                       printfn "meth %s ok, res = %A" meth.Name retActual
                       match constrainReturnValueByShapeInfo syms retActual attr.ReturnShape meth.ReturnParameter loc with 
                       | Ok () -> ()
                       | Error diag -> diags.Add diag

                       // Show extra information about over-constrained variables
                       let moreDiags = syms.GetAdditionalDiagnostics()
                  
                       for (severity, loc, msg) in moreDiags do   
                            diags.Add ({ Severity=severity; LocationStack=[| loc |]; Message = msg; Number=1996 })
                       methCalls.Add(meth, args, Ok retActual)
                    | Error e -> 
                       methCalls.Add(meth, args, Error e)
                       diags.Add e
                finally
                    syms.Pop()
        for diag in diags do  
           printfn "%s" diag.Message
           
        ctorArgs, Ok model, methCalls.ToArray(), diags.ToArray()

/// When added a to model or its methods, indicates that ShapeCheck tooling should analyse the shapes
/// of the construct.
[<AttributeUsage(validOn=AttributeTargets.All, AllowMultiple=true, Inherited=true)>]
type ShapeCheckAttribute internal (given: obj[]) =
    inherit System.Attribute()
    new (argShape1: obj) = ShapeCheckAttribute([| argShape1 |])
    new (argShape1: obj, argShape2: obj) = ShapeCheckAttribute([| argShape1; argShape2 |])
    new (argShape1: obj, argShape2: obj, argShape3: obj) = ShapeCheckAttribute([| argShape1; argShape2; argShape3 |])
    new () = ShapeCheckAttribute([| |] : obj[])

    member val ReturnShape : obj = null with get, set

    member _.GivenArgs = given

    /// 'fslive' invokes this member with the right information and expects exactly this goopo of information
    /// back
    ///
    /// TODO: see if there are standard types somewhere to use for this
    member attr.Invoke(targetType: System.Type, 
             methLocs: (string * string * int * int * int * int)[],
             locFile: string, 
             locStartLine: int, 
             locStartColumn: int, 
             locEndLine: int, 
             locEndColumn: int) 
            : (int (* severity *) * 
               int (* number *) * 
               (string * int * int * int * int)[] *  (* location stack *)
               string (* message*))[] =
        let optionals = true
        let ctors = targetType.GetConstructors()
        let syms = SymScope()
        let ctor = 
            ctors 
            |> Array.tryFind (fun ctor -> 
                ctor.GetParameters() |> Array.exists (fun p -> 
                    let pt = p.ParameterType.ToString()
                    pt.Contains("DiffSharp.Int") || pt.Contains("DiffSharp.Shape")))
            |> function 
               | None -> 
                   printf "couldn't find a model constructor taking Int or Shape parameter, assuming first constructor is target of live check"
                   ctors.[0]
               | Some c -> c

        printfn "attr.GivenArgs = %A" attr.GivenArgs
        let loc = { File = locFile; StartLine = locStartLine; StartColumn = locStartColumn; EndLine = locEndLine; EndColumn= locEndColumn }
        let _, _, _, diags = makeModelAndRunShapeChecks syms optionals ctor attr.GivenArgs loc methLocs
        [| for diag in diags -> 
            let stack = 
                [| for m in diag.LocationStack do
                     (m.File, m.StartLine, m.StartColumn, m.EndLine, m.EndColumn) |]
            (diag.Severity, diag.Number, stack, diag.Message) |]

[<AutoOpen>]
module MoreTools =

    type Model with

        /// Analyses the shapes of a model and prints a report
        static member AnalyseShapes<'T when 'T :> DiffSharp.Model.Model> ([<CallerFilePath>] caller, [<CallerLineNumber>] callerLine, ?optionals: bool) =
            let optionals = defaultArg optionals true
            let dflt = Backend.Default
            try
                Backend.Default <- Backend.ShapeChecking
                let syms = SymScope()
                let ctors = typeof<'T>.GetConstructors()
                let ctor = 
                    ctors 
                    |> Array.tryFind (fun ctor -> 
                        ctor.GetParameters() |> Array.exists (fun p -> 
                            let pt = p.ParameterType.ToString()
                            pt.Contains("DiffSharp.Int")))
                    |> function 
                        | None -> ctors.[0] // failwith "couldn't find a model constructor taking Int parameter"
                        | Some c -> c
                let loc = { File = caller; StartLine = callerLine; StartColumn = 0; EndLine = callerLine; EndColumn= 80 }

                // TODO: use _diags
                let ctorArgs, model, methCalls, _diags = makeModelAndRunShapeChecks syms optionals ctor [| |] loc [| |]
                match model with 
                | Error e -> 
                   printfn "%O: error DS1998 - %s" loc e.Message
                | Ok model -> 
                let model = model :?> Model

                printfn ""
                printfn "---------------------"
                let argText = 
                    (ctor.GetParameters(), ctorArgs) 
                    ||> Array.zip
                    |> Array.filter (fun (_p, arg) -> arg <> null) // filter out 'None'
                    |> Array.map (fun (p, arg) -> 
                        // get rid of the Some for F# optional arguments
                        p, (if arg.GetType().FullName.StartsWith("Microsoft.FSharp.Core.FSharpOption`1[") then 
                                (snd (Reflection.FSharpValue.GetUnionFields(arg, arg.GetType()))).[0] 
                            else arg)) // filter out 'None'
                    |> Array.map (fun (p, arg) -> if p.IsOptional then p.Name+"="+string arg else string arg)
                    |> String.concat ","
                printfn "%s(%s)" typeof<'T>.FullName argText
                      
                for (KeyValue(a,b)) in model.parametersDict.values |> Seq.toArray |> Seq.sortBy (fun (KeyValue(a,_)) -> a) do
                    printfn "   %s : %O" a b.value.shapex

                // Probe the forward function for shape behaviour
                for (m, input, res) in methCalls do
                    match res with 
                    | Ok res -> 
                        printfn "   %s(%O) : %O" m.Name input res
                    | Error e -> 
                        printfn "   %s(%O) // Error: %s" m.Name input e.Message
                        //printfn "      %O --> fails\n%s" input.shapex (e.ToString().Split('\r','\n') |> Array.map (fun s -> "        " + s) |> String.concat "\n")
                        () 
            finally
                Backend.Default <- dflt
