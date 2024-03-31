﻿type Literal<'Value> =
    | Pos of 'Value
    | Neg of 'Value

type Clause<'Value> = list<Literal<'Value>>
type Valuation<'Value> = list<Literal<'Value>>
type CNF<'Value> = list<Clause<'Value>>

let neg literal =
    match literal with
    | Pos l -> Neg l
    | Neg l -> Pos l

let propagate literal cnf =
    List.filter (fun clause -> clause <> [ literal ] && (List.contains literal clause |> not)) cnf
    |> List.map (List.filter ((<>) (neg literal)))

let dpll cnf =
    let rec inner cnf valuation =
        if List.isEmpty cnf then
            valuation
        elif List.contains [] cnf then
            []
        else
            let unitClause = List.tryFind (fun clause -> List.length clause = 1) cnf

            if unitClause.IsSome then
                let unitLiteral = unitClause.Value |> List.head
                inner (propagate unitLiteral cnf) (valuation @ [ unitLiteral ])
            else
                let fstLiteral = List.head cnf |> List.head
                let res = inner (propagate fstLiteral cnf) (valuation @ [ fstLiteral ])

                if List.isEmpty res then
                    inner (propagate (neg fstLiteral) cnf) (valuation @ [ neg fstLiteral ])
                else
                    res

    inner cnf []

type DIMACSFile(pathToFile: string) =
    let rawLines = System.IO.File.ReadLines pathToFile
    let noCommentsLines = Seq.skipWhile (fun (n: string) -> n[0] = 'c') rawLines
    let header = (Seq.head noCommentsLines).Split()
    let varsNum = int header[2]
    let clausesNum = int header[3]
    let data = Seq.tail noCommentsLines |> Seq.removeAt clausesNum // Expected last line to be an empty line

    member this.Data = data
    member this.VarsNum = varsNum
    member this.ClausesNum = clausesNum

    member this.ToCNF =

        let lineMapping (line: string) =
            Array.takeWhile ((<>) "0") (line.Split())
            |> Array.map (fun n ->
                let n = n |> int
                if n < 0 then Neg n else Pos n)
            |> Array.toList

        Seq.map lineMapping data |> Seq.toList

let toDIMACSOutput valuation =
    match valuation with
    | [] -> "UNSAT"
    | _ ->
        List.fold
            (fun acc literal ->
                match literal with
                | Neg l -> acc + string (-l) + " "
                | Pos l -> acc + string l + " ")
            "SAT "
            valuation
        + "0"

let solve pathToRead =
    let fileToRead = DIMACSFile(pathToRead)
    let model = dpll fileToRead.ToCNF

    printfn $"%s{toDIMACSOutput model}"

[<EntryPoint>]
let main args =
    solve args[0]
    0
